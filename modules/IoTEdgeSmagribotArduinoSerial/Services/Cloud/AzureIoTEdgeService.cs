using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;

namespace IoTEdgeSmagribotArduinoSerial.Services.Cloud
{
    public class AzureIoTEdgeService : AzureIoTHubServiceBase
    {
        private ModuleClient _ioTHubModuleClient;

        public AzureIoTEdgeService(ILogger logger) : base(logger)
        {
        }
        
        public override async Task Connect()
        {
            Logger.LogDebug($"Connecting to IoT Hub Edge");
            var mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            ITransportSettings[] settings = { mqttSetting };
            
            _ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await _ioTHubModuleClient.OpenAsync().ConfigureAwait(false);
            
            await _ioTHubModuleClient.SetMethodHandlerAsync("SetRelay", SetRelayMethod, null).ConfigureAwait(false);
            await _ioTHubModuleClient.SetMethodHandlerAsync("SetFan", SetFanMethod, null).ConfigureAwait(false);

            await _ioTHubModuleClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChanged, null).ConfigureAwait(false);

            var twin = await _ioTHubModuleClient.GetTwinAsync();
            await OnDesiredPropertyChanged(twin.Properties.Desired, this);
        }

        public override async Task Disconnect()
        {
            Logger.LogDebug($"Disconnecting to IoT Hub");
            await _ioTHubModuleClient.CloseAsync().ConfigureAwait(false);
        }

        protected override Task SendMessage(Message message)
        {
            return _ioTHubModuleClient.SendEventAsync("smagribotarduinoserialOutput", message);
        }

        protected override Task UpdateTwin(TwinCollection newTwinData)
        {
            return _ioTHubModuleClient.UpdateReportedPropertiesAsync(newTwinData);
        }
    }
}