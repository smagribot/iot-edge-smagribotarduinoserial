using Newtonsoft.Json;

namespace IoTEdgeSmagribotArduinoSerial.Models.DeviceProperties
{
    public class ReportedDeviceProperties
    {
        
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}