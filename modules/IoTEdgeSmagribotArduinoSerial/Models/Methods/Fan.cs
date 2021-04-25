using System;

namespace IoTEdgeSmagribotArduinoSerial.Models.Methods
{
    public class Fan
    {
        public int Number { get; set; }
        public int Speed { get; set; }

        public override bool Equals(object obj)
        {
            return obj is Fan fan && Equals(fan);
        }

        protected bool Equals(Fan other)
        {
            return Number == other.Number && Speed == other.Speed;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Number, Speed);
        }
        
        public override string ToString()
        {
            return $"Number: {Number} Speed: {Speed}";
        }
    }
}