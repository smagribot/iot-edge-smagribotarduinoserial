using System;

namespace IoTEdgeSmagribotArduinoSerial.Services.Parser
{
    public interface IDeviceResultParser
    {
        bool ParseBool(string msg);
        string ParseBool(bool boolean);
        float ParseFloat(string msg);
        int ParseInt(string msg);
        bool ParseCommand(string msg);
        Version ParseVersion(string msg);
    }
}