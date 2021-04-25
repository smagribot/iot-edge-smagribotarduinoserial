using System;
using System.IO.Ports;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IoTEdgeSmagribotArduinoSerial.Services.DeviceCommunication
{
    public class SerialCommunicationService : ICommunicationService
    {
        private readonly ILogger _logger;
        private readonly SerialPort _serialPort;
        
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        public SerialCommunicationService(ILogger logger, string portName, int baudRate)
        {
            _logger = logger;
            _serialPort = new SerialPort(portName, baudRate);
            _serialPort.NewLine = "\r\n";
        }
        
        public async Task Connect()
        {
            _logger.LogDebug("SerialCommunicationService.Start");
            if (_serialPort.IsOpen)
                return; 
            await Task.Run(() => _serialPort.Open());
            // Arduino reboots on serial connection, so give some time...
            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        public Task Disconnect()
        {
            _logger.LogDebug("SerialCommunicationService.Stop");
            if (!_serialPort.IsOpen)
                return Task.CompletedTask;
            return Task.Run(() => _serialPort.Close());
        }

        public async Task<string> Send(string command)
        {
            await _semaphore.WaitAsync();

            try
            {
                return await Observable.FromAsync(() => Task.Run(() => _serialPort.WriteLine(command)))
                    .Do(_ => _logger.LogDebug($"Sent serial message: {command}"))
                    .SelectMany(_ => Observable.FromAsync(() => Task.Run(() => _serialPort.ReadLine())))
                    .Do(msg => _logger.LogDebug($"Received serial message: {msg}"))
                    .Timeout(TimeSpan.FromSeconds(30))
                    .Take(1);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}