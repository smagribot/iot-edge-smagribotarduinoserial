using Newtonsoft.Json;

namespace IoTEdgeSmagribotArduinoSerial.Models.Messages
{
    public class SmagritbotArduinoDeviceStatus
    {
        public int FanSpeed { get; set; }
        public bool Fill { get; set; }
        public float Temp { get; set; }
        public float Humidity { get; set; }
        public float WaterTemp { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}