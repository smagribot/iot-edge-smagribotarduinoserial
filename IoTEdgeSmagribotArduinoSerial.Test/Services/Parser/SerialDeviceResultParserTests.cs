using System;
using IoTEdgeSmagribotArduinoSerial.Services.Parser;
using Xunit;

namespace IoTEdgeSmagribotArduinoSerial.Test.Services.Parser
{
    public class SerialDeviceResultParserTests
    {
        private readonly SerialDeviceResultParser _sut;

        protected SerialDeviceResultParserTests()
        {
            _sut = new SerialDeviceResultParser();
        }

        public class ParseBoolTestsStringToBool : SerialDeviceResultParserTests
        {
            [Fact]
            public void Should_return_true()
            {
                Assert.True(_sut.ParseBool("1"));
            }

            [Fact]
            public void Should_return_false()
            {
                Assert.False(_sut.ParseBool("0"));
            }

            [Theory]
            [InlineData(null)]
            [InlineData("")]
            [InlineData("Should throw FormatException")]
            [InlineData("-1")]
            public void Should_throw_FormatException(string msg)
            {
                Assert.Throws<FormatException>(() => _sut.ParseBool(msg));
            }
        }

        public class ParseBoolTestsBoolToString : SerialDeviceResultParserTests
        {
            [Fact]
            public void Should_return_true()
            {
                Assert.Equal("1", _sut.ParseBool(true));
            }

            [Fact]
            public void Should_return_false()
            {
                Assert.Equal("0", _sut.ParseBool(false));
            }
        }

        public class ParseFloatTests : SerialDeviceResultParserTests
        {
            [Theory]
            [InlineData("1.0", 1.0f)]
            [InlineData("-1.0", -1.0f)]
            [InlineData("0.0", 0f)]
            [InlineData("1.9999", 1.9999f)]
            public void Should_return_float_value(string msg, float expected)
            {
                Assert.Equal(expected, _sut.ParseFloat(msg));
            }
            
            [Theory]
            [InlineData(null)]
            [InlineData("")]
            [InlineData("Should throw FormatException")]
            [InlineData("1.0.0")]
            public void Should_throw_FormatException(string msg)
            {
                Assert.Throws<FormatException>(() => _sut.ParseFloat(msg));
            }
        }

        public class ParseIntTests : SerialDeviceResultParserTests
        {
            [Theory]
            [InlineData("1", 1)]
            [InlineData("-1", -1)]
            [InlineData("0", 0)]
            public void Should_return_int_value(string msg, int expected)
            {
                Assert.Equal(expected, _sut.ParseInt(msg));
            }
            
            [Theory]
            [InlineData(null)]
            [InlineData("")]
            [InlineData("Should throw FormatException")]
            [InlineData("1.0.0")]
            public void Should_throw_FormatException(string msg)
            {
                Assert.Throws<FormatException>(() => _sut.ParseInt(msg));
            }
        }

        public class ParseCommandTests : SerialDeviceResultParserTests
        {
            [Theory]
            [InlineData("OK", true)]
            [InlineData("ok", true)]
            [InlineData("Ok", true)]
            [InlineData("Error", false)]
            [InlineData("ERROR", false)]
            [InlineData("error", false)]
            [InlineData("", false)]
            [InlineData(null, false)]
            [InlineData("nope", false)]
            public void Should_return_bool_value(string msg, bool expected)
            {
                Assert.Equal(expected, _sut.ParseCommand(msg));
            }
        }

        public class ParseVersion : SerialDeviceResultParserTests
        {
            [Fact]
            public void Should_return_version_value()
            {
                Assert.Equal(Version.Parse("1.0.0"), _sut.ParseVersion("1.0.0"));
            }
            
            [Theory]
            [InlineData("")]
            [InlineData("Should throw Exception")]
            public void Should_throw_ArgumentException<T>(string msg)
            {
                Assert.Throws<ArgumentException>(() => _sut.ParseVersion(msg));
            }
            
            [Theory]
            [InlineData(null)]
            public void Should_throw_ArgumentNullException<T>(string msg)
            {
                Assert.Throws<ArgumentNullException>(() => _sut.ParseVersion(msg));
            }
        }
    }
}