using System.Threading.Tasks;

namespace IoTEdgeSmagribotArduinoSerial.Services.DeviceCommunication
{
    public interface ICommunicationService
    {
        Task Connect();
        Task Disconnect();
        Task<string> Send(string command);
    }
}