using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using IoTEdgeSmagribotArduinoSerial.Models.Messages;
using IoTEdgeSmagribotArduinoSerial.Services.Cloud;
using IoTEdgeSmagribotArduinoSerial.Services.Device;
using IoTEdgeSmagribotArduinoSerial.Services.Scheduler;
using Microsoft.Extensions.Logging;

namespace IoTEdgeSmagribotArduinoSerial
{
    public class Runner : IDisposable
    {
        private readonly CompositeDisposable _compositeDisposable = new CompositeDisposable();

        private readonly ILogger _logger;
        private readonly ISchedulerProvider _schedulerProvider;
        private readonly ICloudService _cloudService;
        private readonly IDeviceService _deviceService;

        private IDisposable _timedUpdateDisposable;
        private TimeSpan _currentTimerInterval = TimeSpan.Zero;

        public Runner(
            ILogger logger,
            ISchedulerProvider schedulerProvider,
            ICloudService cloudService,
            IDeviceService deviceService
        )
        {
            _logger = logger;
            _schedulerProvider = schedulerProvider;
            _cloudService = cloudService;
            _deviceService = deviceService;
        }

        public void Run()
        {
            _logger.LogInformation("Starting runner");

            ObserveSetFan();
            ObserveSetRelay();

            var connectedObservable = ConnectToSerialAndCloud();
            SendStatusAndHearForPeriodUpdates(connectedObservable);
        }

        private void ObserveSetFan()
        {
            var setFanObservable = _cloudService.SetFan()
                .SelectMany(fan => _deviceService.SetFan(fan))
                .RetryWhen(observable =>
                    observable
                        .Do(ex => _logger.LogWarning($"Setfan command throw {ex} subscribing again"))
                        .Zip(Observable.Return(0).Delay(TimeSpan.FromSeconds(30), _schedulerProvider.NewThread),
                            (exception, i) => i)
                        .Do(_ => _logger.LogInformation("Setfan command subscribing again"))
                )
                .SubscribeOn(_schedulerProvider.NewThread)
                .Subscribe(
                    fanSetSuccess => _logger.LogInformation($"Set fan to new value successfully: {fanSetSuccess}"),
                    err => _logger.LogError($"Got error while setting fan to new value: {err}"),
                    () => _logger.LogInformation("Listing for set fan commands finished"));
            _compositeDisposable.Add(setFanObservable);
        }
        
        private void ObserveSetRelay()
        {
            var setRelayObservable = _cloudService.SetRelay()
                .SelectMany(relay => _deviceService.SetRelay(relay))
                .RetryWhen(observable =>
                    observable
                        .Do(ex => _logger.LogWarning($"SetRelay command throw {ex} subscribing again"))
                        .Zip(Observable.Return(0).Delay(TimeSpan.FromSeconds(30), _schedulerProvider.NewThread),
                            (exception, i) => i)
                        .Do(_ => _logger.LogInformation("SetRelay command subscribing again"))
                )
                .SubscribeOn(_schedulerProvider.NewThread)
                .Subscribe(
                    setRelaySuccess => _logger.LogInformation($"Set relay to new value successfully: {setRelaySuccess}"),
                    err => _logger.LogError($"Got error while setting relay to new value: {err}"),
                    () => _logger.LogInformation("Listing for set relay commands finished"));
            _compositeDisposable.Add(setRelayObservable);
        }

        private IObservable<Unit> ConnectToSerialAndCloud()
        {
            var connectedObservable = Observable.FromAsync(async () => await _deviceService.Connect())
                .SelectMany(_ => Observable.FromAsync(async () => await _cloudService.Connect()));
            return connectedObservable;
        }

        private void SendStatusAndHearForPeriodUpdates(IObservable<Unit> connectedObservable)
        {
            // Get TelemetryConfig.Period from reported properties to update StartIntervalledStatusUpdate with new TimeSpan
            var newIntervalObservable = _cloudService.GetDesiredProperties()
                .Where(properties => properties?.TelemetryConfig != null)
                .Select(properties => properties.TelemetryConfig.PublishInterval)
                .Select(TimeSpan.FromSeconds)
                // When device and cloud are connected, start once with default interval, in case reported properties
                // doesn't contain TelemetryConfig.Period or they aren't reported yet
                .Merge(connectedObservable.Select(_ => TimeSpan.FromMinutes(15)))
                .Where(timespan => timespan != _currentTimerInterval)
                .Do(timespan => _currentTimerInterval = timespan)
                .SubscribeOn(_schedulerProvider.NewThread)
                .Subscribe(timespan =>
                    {
                        _logger.LogInformation(
                            $"Setup new interval for device status. Interval is {timespan.TotalMinutes} minutes");
                        StartIntervalledStatusUpdate(timespan);
                    },
                    err => { _logger.LogError($"Got error while listining for TelemetryConfig.Period: {err}"); },
                    () => { _logger.LogInformation("Listing for TelemetryConfig.Period finished"); });

            _compositeDisposable.Add(newIntervalObservable);
        }

        private void StartIntervalledStatusUpdate(TimeSpan timespan)
        {
            _timedUpdateDisposable?.Dispose();
            _timedUpdateDisposable = Observable.Interval(timespan, _schedulerProvider.NewThread)
                .Merge(Observable.Return(0L))
                .SelectMany(_ => _deviceService.GetStatus())
                .SelectMany(SendStatusToCloudWithRetry)
                .SubscribeOn(_schedulerProvider.NewThread)
                .Subscribe(status => { _logger.LogInformation($"Updated status: {status}"); },
                    err => { _logger.LogError($"Device status interval got error: {err}"); },
                    () => { _logger.LogInformation("Device status interval finished"); });
        }

        private IObservable<SmagritbotArduinoDeviceStatus> SendStatusToCloudWithRetry(
            SmagritbotArduinoDeviceStatus status)
        {
            return Observable.FromAsync(() => _cloudService.SendStatusMessage(status))
                .Select(_ => status)
                .RetryWhen(observable =>
                    observable
                        .Do(ex => _logger.LogWarning($"SendStatusMessage throw {ex} trying again"))
                        .Zip(Observable.Return(0).Delay(TimeSpan.FromSeconds(30), _schedulerProvider.NewThread),
                            (exception, i) => i)
                        .Do(_ => _logger.LogInformation("SendStatusMessage trying again"))
                );
        }

        public void Dispose()
        {
            _logger.LogInformation("Disposing runner");
            _timedUpdateDisposable?.Dispose();
            _compositeDisposable?.Dispose();
        }
    }
}