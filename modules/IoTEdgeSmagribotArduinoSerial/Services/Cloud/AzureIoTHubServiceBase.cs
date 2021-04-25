using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using IoTEdgeSmagribotArduinoSerial.Models.DeviceProperties;
using IoTEdgeSmagribotArduinoSerial.Models.Messages;
using IoTEdgeSmagribotArduinoSerial.Models.Methods;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace IoTEdgeSmagribotArduinoSerial.Services.Cloud
{
    public abstract class AzureIoTHubServiceBase : ICloudService
    {
        protected readonly ILogger Logger;
        private readonly BehaviorSubject<DesiredDeviceProperties> _desiredDevicePropertiesSubject = new BehaviorSubject<DesiredDeviceProperties>(null);
        private readonly Subject<Relay> _relaySubject = new Subject<Relay>();
        private readonly Subject<Fan> _fanSubject = new Subject<Fan>();

        protected AzureIoTHubServiceBase(ILogger logger)
        {
            Logger = logger;
        }
        
        public abstract Task Connect();
        public abstract Task Disconnect();
        
        protected abstract Task SendMessage(Message message);

        protected abstract Task UpdateTwin(TwinCollection newTwinData);

        public Task SendStatusMessage(SmagritbotArduinoDeviceStatus status)
        {
            Logger.LogDebug($"Sending status to IoT Hub:\n{status}");
            var eventMessage = JsonConvert.SerializeObject(status);
            var message = new Message(Encoding.UTF8.GetBytes(eventMessage))
            {
                ContentType = "application/json", 
                ContentEncoding = "utf-8",
            };
            message.Properties.Add("content-type", "application/edge-samgribotarduinoserial-json");
            return SendMessage(message);
        }
        
        public Task SendEvent(string eventMessage)
        {
            Logger.LogDebug($"Sending event to IoT Hub:\n{eventMessage}");
            var message = new Message(Encoding.UTF8.GetBytes(eventMessage));
            return SendMessage(message);
        }
        
        public Task UpdateProperties(ReportedDeviceProperties updatedProperties)
        {
            var propertiesAsJson = JsonConvert.SerializeObject(updatedProperties, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            Logger.LogDebug($"Sending updated properties to IoT Hub:\n{propertiesAsJson}");
            var newTwinData = new TwinCollection(propertiesAsJson);
            return UpdateTwin(newTwinData);
        }
        
        protected Task<MethodResponse> SetRelayMethod(MethodRequest methodRequest, object userContext)
        {
            Logger.LogDebug($"{nameof(SetRelayMethod)} was called");

            var relay = JsonConvert.DeserializeObject<Relay>(methodRequest.DataAsJson);
            _relaySubject.OnNext(relay);
            
            //TODO: Needs proper result!
            return Task.FromResult(new MethodResponse(new byte[0], 200));
        }

        protected Task<MethodResponse> SetFanMethod(MethodRequest methodRequest, object userContext)
        {
            Logger.LogDebug($"{nameof(SetFanMethod)} was called");
            
            var fan = JsonConvert.DeserializeObject<Fan>(methodRequest.DataAsJson);
            _fanSubject.OnNext(fan);

            //TODO: Needs proper result!
            return Task.FromResult(new MethodResponse(new byte[0], 200));
        }

        protected Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object userContext)
        {
            Logger.LogDebug($"Desired property changed:\n{desiredProperties.ToJson()}");

            var desiredDeviceProperties =
                JsonConvert.DeserializeObject<DesiredDeviceProperties>(desiredProperties.ToJson());
            _desiredDevicePropertiesSubject.OnNext(desiredDeviceProperties);

            return Task.CompletedTask;
        }

        public IObservable<Relay> SetRelay()
        {
            return _relaySubject.Publish().RefCount();
        }

        public IObservable<Fan> SetFan()
        {
            return _fanSubject.Publish().RefCount();
        }

        public IObservable<string> CloudMessage()
        {
            return Observable.Empty<string>();
        }

        public IObservable<DesiredDeviceProperties> GetDesiredProperties()
        {
            return _desiredDevicePropertiesSubject.Publish().RefCount();
        }
    }
}