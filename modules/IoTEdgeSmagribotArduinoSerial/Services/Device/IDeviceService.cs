using System;
using System.Threading.Tasks;
using IoTEdgeSmagribotArduinoSerial.Models.Messages;
using IoTEdgeSmagribotArduinoSerial.Models.Methods;

namespace IoTEdgeSmagribotArduinoSerial.Services.Device
{
    public interface IDeviceService
    {
        Task Connect();
        Task Disconnect();
        Task<SmagritbotArduinoDeviceStatus> GetStatus();
        Task<Version> GetFirmware();

        Task<bool> SetFan(Fan fan);
        Task<bool> SetRelay(Relay relay);
    }
}