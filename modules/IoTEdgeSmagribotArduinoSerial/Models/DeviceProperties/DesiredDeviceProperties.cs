using Newtonsoft.Json;

namespace IoTEdgeSmagribotArduinoSerial.Models.DeviceProperties
{
    public class TelemetryConfig
    {
        public double PublishInterval { get; set; }
    }

    public class DesiredDeviceProperties
    {
        public TelemetryConfig TelemetryConfig { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}