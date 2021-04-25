using System;
using System.Threading.Tasks;
using IoTEdgeSmagribotArduinoSerial.Models.DeviceProperties;
using IoTEdgeSmagribotArduinoSerial.Models.Messages;
using IoTEdgeSmagribotArduinoSerial.Models.Methods;

namespace IoTEdgeSmagribotArduinoSerial.Services.Cloud
{
    public interface ICloudService
    {
        Task Connect();
        Task Disconnect();

        Task SendStatusMessage(SmagritbotArduinoDeviceStatus status);
        Task UpdateProperties(ReportedDeviceProperties updatedProperties);

        IObservable<Relay> SetRelay();
        IObservable<Fan> SetFan();

        IObservable<string> CloudMessage();
        IObservable<DesiredDeviceProperties> GetDesiredProperties();
    }
}