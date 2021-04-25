using System;

namespace IoTEdgeSmagribotArduinoSerial.Services.Parser
{
    public class SerialDeviceResultParser : IDeviceResultParser
    {
        public bool ParseBool(string msg)
        {
            return msg switch
            {
                "1" => true,
                "0" => false,
                _ => throw new FormatException($"Can't parse {msg} to bool!")
            };
        }

        public string ParseBool(bool boolean)
        {
            return boolean ? "1" : "0";
        }

        public float ParseFloat(string msg)
        {
            if (float.TryParse(msg, out var parsedValue))
            {
                return parsedValue;
            }

            throw new FormatException($"Can't parse {msg} to float!");
        }

        public int ParseInt(string msg)
        {
            if (int.TryParse(msg, out var parsedValue))
            {
                return parsedValue;
            }

            throw new FormatException($"Can't parse {msg} to int!");
        }

        public bool ParseCommand(string msg)
        {
            return msg?.ToLower() == "ok";
        }

        public Version ParseVersion(string version)
        {
            return Version.Parse(version);
        }
    }
}