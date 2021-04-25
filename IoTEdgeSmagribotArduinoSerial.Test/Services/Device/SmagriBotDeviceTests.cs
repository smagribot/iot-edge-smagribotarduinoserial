using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using IoTEdgeSmagribotArduinoSerial.Models.DeviceProperties;
using IoTEdgeSmagribotArduinoSerial.Models.Methods;
using IoTEdgeSmagribotArduinoSerial.Services.Cloud;
using IoTEdgeSmagribotArduinoSerial.Services.Device;
using IoTEdgeSmagribotArduinoSerial.Services.DeviceCommunication;
using IoTEdgeSmagribotArduinoSerial.Services.Parser;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IoTEdgeSmagribotArduinoSerial.Test.Services.Device
{
    public class SmagriBotDeviceTests
    {
        private readonly SmagriBotDevice _sut;
        
        private readonly Mock<ICommunicationService> _communicationServiceMock;
        private readonly Mock<IDeviceResultParser> _deviceResultParserMock;

        protected SmagriBotDeviceTests()
        {
            _communicationServiceMock = new Mock<ICommunicationService>();
            _deviceResultParserMock = new Mock<IDeviceResultParser>();

            _communicationServiceMock.Setup(m => m.Send("getfill")).ReturnsAsync("1");
            _communicationServiceMock.Setup(m => m.Send("getdht")).ReturnsAsync("60.74 21.21");
            _communicationServiceMock.Setup(m => m.Send("getfan")).ReturnsAsync("42");
            _communicationServiceMock.Setup(m => m.Send("getwatertmp")).ReturnsAsync("18.45");
            _communicationServiceMock.Setup(m => m.Send("setfan 40")).ReturnsAsync("ok");
            _communicationServiceMock.Setup(m => m.Send("setrelay 0 1")).ReturnsAsync("ok");
            _communicationServiceMock.Setup(m => m.Send("getfw")).ReturnsAsync("0.0.1");

            _deviceResultParserMock.Setup(m => m.ParseBool("1")).Returns(true);
            _deviceResultParserMock.Setup(m => m.ParseBool(true)).Returns("1");
            _deviceResultParserMock.Setup(m => m.ParseInt("42")).Returns(42);
            _deviceResultParserMock.Setup(m => m.ParseFloat("21.21")).Returns(21.21f);
            _deviceResultParserMock.Setup(m => m.ParseFloat("60.74")).Returns(60.74f);
            _deviceResultParserMock.Setup(m => m.ParseFloat("18.45")).Returns(18.45f);
            _deviceResultParserMock.Setup(m => m.ParseCommand("ok")).Returns(true);
            _deviceResultParserMock.Setup(m => m.ParseCommand("error")).Returns(false);
            _deviceResultParserMock.Setup(m => m.ParseVersion("0.0.1")).Returns(Version.Parse("0.0.1"));

            _sut = new SmagriBotDevice(
                new Mock<ILogger>().Object,
                _communicationServiceMock.Object,
                _deviceResultParserMock.Object
            );
        }

        public class Connect : SmagriBotDeviceTests
        {
            [Fact]
            public async Task Should_call_CommunicationService_Connect()
            {
                await _sut.Connect();
                _communicationServiceMock.Verify(m => m.Connect());
            }
        }
        
        public class Disconnect : SmagriBotDeviceTests
        {
            [Fact]
            public async Task Should_call_CommunicationService_Disconnect()
            {
                await _sut.Disconnect();
                
                _communicationServiceMock.Verify(m => m.Disconnect());
            }
        }
        
        public class GetStatus : SmagriBotDeviceTests
        {
            [Fact]
            public async Task Should_return_DeviceStatus()
            {
                var result = await _sut.GetStatus();
                
                Assert.NotNull(result);
            }

            [Fact]
            public async Task Should_return_DeviceStatus_with_Fill_true()
            {
                var result = await _sut.GetStatus();
                
                Assert.True(result.Fill);
                _communicationServiceMock.Verify(m => m.Send("getfill"));
                _deviceResultParserMock.Verify(m => m.ParseBool("1"));
            }

            [Fact]
            public async Task Should_return_DeviceStatus_with_Temp_21_21()
            {
                var result = await _sut.GetStatus();
                
                Assert.Equal(21.21f, result.Temp);
                
                _communicationServiceMock.Verify(m => m.Send("getdht"));
                _deviceResultParserMock.Verify(m => m.ParseFloat("21.21"));
            }
            
            [Fact]
            public async Task Should_return_DeviceStatus_with_Humidity_60_74()
            {
                var result = await _sut.GetStatus();
                
                Assert.Equal(60.74f, result.Humidity);
                
                _communicationServiceMock.Verify(m => m.Send("getdht"));
                _deviceResultParserMock.Verify(m => m.ParseFloat("60.74"));
            }
            
            [Fact]
            public async Task Should_return_DeviceStatus_with_WaterTemp_18_45()
            {
                var result = await _sut.GetStatus();
                
                Assert.Equal(18.45f, result.WaterTemp);
                
                _communicationServiceMock.Verify(m => m.Send("getwatertmp"));
                _deviceResultParserMock.Verify(m => m.ParseFloat("18.45"));
            }
            
            [Fact(Skip = "FanSpeed is currently not implemented on the hardware")]
            public async Task Should_return_DeviceStatus_with_FanSpeed_42()
            {
                var result = await _sut.GetStatus();
                
                Assert.Equal(42, result.FanSpeed);
                
                _communicationServiceMock.Verify(m => m.Send("getfan"));
                _deviceResultParserMock.Verify(m => m.ParseInt("42"));
            }
        }

        public class SetFan : SmagriBotDeviceTests
        {
            [Fact]
            public async Task Should_send_return_True_when_setfan_command_return_Ok()
            {
                var fan = new Fan {Number = 0, Speed = 40};
               
                var result = await _sut.SetFan(fan);
                
                Assert.True(result);
            }
            
            [Fact]
            public async Task Should_send_return_False_when_setfan_command_return_Error()
            {
                var fan = new Fan {Number = 0, Speed = 40};
                _communicationServiceMock.Setup(m => m.Send("setfan 40"))
                    .ReturnsAsync("error");

                var result = await _sut.SetFan(fan);
                
                Assert.False(result);
            }
            
            [Fact]
            public async Task Should_send_setfan_command_with_parameter()
            {
                var fan = new Fan {Number = 0, Speed = 40};
                
                await _sut.SetFan(fan);

                _communicationServiceMock.Verify(m => m.Send("setfan 40"));
            }

            [Fact]
            public async Task Should_parse_result_of_CommunicationService_result()
            {
                var fan = new Fan {Number = 0, Speed = 40};
                
                await _sut.SetFan(fan);
                
                _deviceResultParserMock.Verify(m => m.ParseCommand("ok"));
            }
        }

        public class SetRelay : SmagriBotDeviceTests
        {
            [Fact]
            public async Task Should_return_True_when_setrelay_command_returns_ok()
            {
                var relay = new Relay {Number = 0, On = true};
               
                var result = await _sut.SetRelay(relay);
                
                Assert.True(result);
            }
            
            [Fact]
            public async Task Should_return_False_when_setrelay_command_returns_Error()
            {
                var relay = new Relay {Number = 0, On = true};
                _communicationServiceMock.Setup(m => m.Send("setrelay 0 1"))
                    .ReturnsAsync("error");

                var result = await _sut.SetRelay(relay);
                
                Assert.False(result);
            }
            
            [Fact]
            public async Task Should_send_setrelay_command_with_parameter()
            {
                var relay = new Relay {Number = 0, On = true};
                
                await _sut.SetRelay(relay);

                _communicationServiceMock.Verify(m => m.Send("setrelay 0 1"));
            }

            [Fact]
            public async Task Should_parse_result_of_CommunicationService_result()
            {
                var relay = new Relay {Number = 0, On = true};
                
                await _sut.SetRelay(relay);
                
                _deviceResultParserMock.Verify(m => m.ParseCommand("ok"));
            }
        }

        public class GetFirmware : SmagriBotDeviceTests
        {
            [Fact]
            public async Task Should_send_fw_command()
            {   
                await _sut.GetFirmware();

                _communicationServiceMock.Verify(m => m.Send("getfw"));
            }
            
            [Fact]
            public async Task Should_parse_result_of_CommunicationService_result()
            {
                 await _sut.GetFirmware();
                
                _deviceResultParserMock.Verify(m => m.ParseVersion("0.0.1"));
            }
            
            [Fact]
            public async Task Should_return_Version_1_0_0_when_setrelay_command_returns_ok()
            {  
                var result = await _sut.GetFirmware();
                
                Assert.Equal(Version.Parse("0.0.1"),result);
            }
        }
    }
}