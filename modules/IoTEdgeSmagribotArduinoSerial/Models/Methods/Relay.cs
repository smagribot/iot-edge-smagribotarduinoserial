using System;

namespace IoTEdgeSmagribotArduinoSerial.Models.Methods
{
    public class Relay
    {
        public byte Number { get; set; }
        public bool On { get; set; }

        public override bool Equals(object obj)
        {
            return obj is Relay relay && Equals(relay);
        }

        protected bool Equals(Relay other)
        {
            return Number == other.Number && On == other.On;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Number, On);
        }

        public override string ToString()
        {
            return $"Number: {Number} On: {On}";
        }
    }
}