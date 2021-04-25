using System;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using IoTEdgeSmagribotArduinoSerial.Models.DeviceProperties;
using IoTEdgeSmagribotArduinoSerial.Models.Messages;
using IoTEdgeSmagribotArduinoSerial.Models.Methods;
using IoTEdgeSmagribotArduinoSerial.Services.Cloud;
using IoTEdgeSmagribotArduinoSerial.Services.Device;
using IoTEdgeSmagribotArduinoSerial.Test.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IoTEdgeSmagribotArduinoSerial.Test
{
    public class RunnerTests
    {
        private readonly Runner _sut;

        private readonly TestSchedulerProvider _schedulerProvider = new TestSchedulerProvider();
        private readonly Mock<ILogger> _loggerMock = new Mock<ILogger>();
        private readonly Mock<ICloudService> _cloudServiceMock = new Mock<ICloudService>();
        private readonly Mock<IDeviceService> _deviceServiceMock = new Mock<IDeviceService>();
        
        private readonly Subject<DesiredDeviceProperties> _devicePropertiesSubject = new Subject<DesiredDeviceProperties>();
        private readonly Subject<Fan> _fanSubject = new Subject<Fan>();
        private readonly Subject<Relay> _relaySubject = new Subject<Relay>();


        protected RunnerTests()
        {
            _cloudServiceMock.Setup(m => m.GetDesiredProperties())
                .Returns(_devicePropertiesSubject);
            _cloudServiceMock.Setup(m => m.SetFan())
                .Returns(_fanSubject);
            _cloudServiceMock.Setup(m => m.SetRelay())
                .Returns(_relaySubject);

            _sut = new Runner(
                _loggerMock.Object,
                _schedulerProvider,
                _cloudServiceMock.Object,
                _deviceServiceMock.Object
            );
        }

        public class Run : RunnerTests
        {
            [Fact]
            public void Should_connect_to_device()
            {
                _sut.Run();

                _schedulerProvider.AdvanceTo(1);
                _deviceServiceMock.Verify(m => m.Connect());
            }
            
            [Fact]
            public void Should_connect_to_cloud()
            {
                _sut.Run();

                _schedulerProvider.AdvanceTo(1);
                _cloudServiceMock.Verify(m => m.Connect());
            }

            [Fact]
            public void Should_not_connect_to_cloud_when_cant_connect_to_device()
            {
                _deviceServiceMock.Setup(m => m.Connect()).ThrowsAsync(new Exception());
                
                _sut.Run();

                _schedulerProvider.AdvanceTo(1);
                _deviceServiceMock.Verify(m => m.Connect());
                _cloudServiceMock.Verify(m => m.Connect(), Times.Never);
            }

            [Fact]
            public void Should_get_every_15_minutes_device_status_starting_at_0_by_default()
            {
                _sut.Run();
                
                _schedulerProvider.AdvanceTo(2);
                _deviceServiceMock.Verify(m => m.GetStatus(), Times.Exactly(1));
                
                _schedulerProvider.AdvanceBy(TimeSpan.FromMinutes(15).Ticks);
                _deviceServiceMock.Verify(m => m.GetStatus(), Times.Exactly(2));
                
                _schedulerProvider.AdvanceBy(TimeSpan.FromMinutes(15).Ticks);
                _deviceServiceMock.Verify(m => m.GetStatus(), Times.Exactly(3));
            }
            
            [Fact]
            public void Should_not_get_device_status_when_device_isnt_connected()
            {
                var deviceConnectTaskCompliationSource = new TaskCompletionSource<bool>();
                _deviceServiceMock.Setup(m => m.Connect()).Returns(() => deviceConnectTaskCompliationSource.Task);
                
                _sut.Run();
                
                _schedulerProvider.AdvanceTo(2);
                _deviceServiceMock.Verify(m => m.GetStatus(), Times.Never);
            }
            
            [Fact]
            public void Should_not_get_device_status_when_device_cant_connect_to_device()
            {
                _deviceServiceMock.Setup(m => m.Connect()).ThrowsAsync(new Exception());
                
                _sut.Run();
                
                _schedulerProvider.AdvanceTo(2);
                _deviceServiceMock.Verify(m => m.GetStatus(), Times.Never);
            }

            [Fact]
            public void Should_send_status_to_cloud()
            {
                var deviceStatus = new SmagritbotArduinoDeviceStatus();
                _deviceServiceMock.Setup(m => m.GetStatus())
                    .ReturnsAsync(deviceStatus);
                
                _sut.Run();
                
                _schedulerProvider.AdvanceTo(2);
                _cloudServiceMock.Verify(m => m.SendStatusMessage(deviceStatus), Times.Exactly(1));
            }
            
            [Fact]
            public void Should_not_send_status_to_cloud_when_status_cant_be_retrieved()
            {
                _deviceServiceMock.Setup(m => m.GetStatus())
                    .ThrowsAsync(new Exception());
                
                _sut.Run();
                
                _schedulerProvider.AdvanceTo(2);
                _cloudServiceMock.Verify(m => m.SendStatusMessage(It.IsAny<SmagritbotArduinoDeviceStatus>()), Times.Never);
            }

            [Fact]
            public void Should_retry_sending_status_cloud_when_it_fails_after_30_sec()
            {
                var deviceStatus = new SmagritbotArduinoDeviceStatus();
                _deviceServiceMock.Setup(m => m.GetStatus())
                    .ReturnsAsync(deviceStatus);

                _cloudServiceMock.Setup(m => m.SendStatusMessage(It.IsAny<SmagritbotArduinoDeviceStatus>()))
                    .ThrowsAsync(new Exception());

                _sut.Run();
                _schedulerProvider.AdvanceTo(2);
                _cloudServiceMock.Verify(m => m.SendStatusMessage(deviceStatus), Times.Exactly(1));
                
                _schedulerProvider.AdvanceBy(TimeSpan.FromSeconds(15).Ticks);
                _cloudServiceMock.Verify(m => m.SendStatusMessage(deviceStatus), Times.Exactly(1));
                
                _schedulerProvider.AdvanceBy(TimeSpan.FromSeconds(15).Ticks);
                _cloudServiceMock.Verify(m => m.SendStatusMessage(deviceStatus), Times.Exactly(2));
            }

            [Fact]
            public void Should_listen_for_reported_properties()
            {
                _sut.Run();
                
                _schedulerProvider.AdvanceTo(2);
                _cloudServiceMock.Verify(m => m.GetDesiredProperties());
            }
            
            [Fact]
            public void Should_update_device_status_timer_when_new_period_is_set()
            {
                var devPropertiesWithNewPeriod = new DesiredDeviceProperties
                {
                    TelemetryConfig = new TelemetryConfig
                    {
                        PublishInterval = TimeSpan.FromMinutes(5).TotalSeconds
                    }
                };
                
                _sut.Run();

                _schedulerProvider.AdvanceTo(2);
                _cloudServiceMock.Verify(m => m.SendStatusMessage(It.IsAny<SmagritbotArduinoDeviceStatus>()), Times.Exactly(1));

                _schedulerProvider.AdvanceBy(TimeSpan.FromMinutes(15).Ticks);
                _deviceServiceMock.Verify(m => m.GetStatus(), Times.Exactly(2));
                
                //Update to new time
                _devicePropertiesSubject.OnNext(devPropertiesWithNewPeriod);
                
                _schedulerProvider.AdvanceBy(1);
                _deviceServiceMock.Verify(m => m.GetStatus(), Times.Exactly(3));
                
                _schedulerProvider.AdvanceBy(TimeSpan.FromMinutes(5).Ticks);
                _deviceServiceMock.Verify(m => m.GetStatus(), Times.Exactly(4));
                
                _schedulerProvider.AdvanceBy(TimeSpan.FromMinutes(5).Ticks);
                _deviceServiceMock.Verify(m => m.GetStatus(), Times.Exactly(5));
                
                _schedulerProvider.AdvanceBy(TimeSpan.FromMinutes(5).Ticks);
                _deviceServiceMock.Verify(m => m.GetStatus(), Times.Exactly(6));
            }

            [Fact]
            public void Should_not_update_device_status_timer_when_it_doesnt_changed()
            {
                var devPropertiesWithNewPeriod = new DesiredDeviceProperties
                {
                    TelemetryConfig = new TelemetryConfig
                    {
                        PublishInterval = TimeSpan.FromMinutes(15).TotalSeconds
                    }
                };
                
                _sut.Run();

                _schedulerProvider.AdvanceTo(2);
                _cloudServiceMock.Verify(m => m.SendStatusMessage(It.IsAny<SmagritbotArduinoDeviceStatus>()), Times.Exactly(1));

                _schedulerProvider.AdvanceBy(TimeSpan.FromMinutes(15).Ticks);
                _deviceServiceMock.Verify(m => m.GetStatus(), Times.Exactly(2));
                
                //Update to new time
                _devicePropertiesSubject.OnNext(devPropertiesWithNewPeriod);
                
                _schedulerProvider.AdvanceBy(1);
                _deviceServiceMock.Verify(m => m.GetStatus(), Times.Exactly(2));
                
                _schedulerProvider.AdvanceBy(TimeSpan.FromMinutes(15).Ticks);
                _deviceServiceMock.Verify(m => m.GetStatus(), Times.Exactly(3));
            }
            
            [Fact]
            public void Should_only_listen_to_device_properties_with_TelemetryConfig()
            {
                var devPropertiesWithNewPeriod = new DesiredDeviceProperties
                {
                    TelemetryConfig = new TelemetryConfig
                    {
                        PublishInterval = TimeSpan.FromMinutes(5).TotalSeconds
                    }
                };
                
                _sut.Run();

                _schedulerProvider.AdvanceTo(2);
                _cloudServiceMock.Verify(m => m.SendStatusMessage(It.IsAny<SmagritbotArduinoDeviceStatus>()), Times.Exactly(1));

                _schedulerProvider.AdvanceBy(TimeSpan.FromMinutes(15).Ticks);
                _deviceServiceMock.Verify(m => m.GetStatus(), Times.Exactly(2));
                
                //Update without TelemetryConfig
                _devicePropertiesSubject.OnNext(new DesiredDeviceProperties());
                
                _schedulerProvider.AdvanceBy(1);
                _deviceServiceMock.Verify(m => m.GetStatus(), Times.Exactly(2));
                
                _schedulerProvider.AdvanceBy(TimeSpan.FromMinutes(15).Ticks);
                _deviceServiceMock.Verify(m => m.GetStatus(), Times.Exactly(3));
                
                //Update with TelemetryConfig
                _devicePropertiesSubject.OnNext(devPropertiesWithNewPeriod);
                
                _schedulerProvider.AdvanceBy(1);
                _deviceServiceMock.Verify(m => m.GetStatus(), Times.Exactly(4));
                
                _schedulerProvider.AdvanceBy(TimeSpan.FromMinutes(5).Ticks);
                _deviceServiceMock.Verify(m => m.GetStatus(), Times.Exactly(5));
            }
        }

        public class SetFan : RunnerTests
        {
            [Fact]
            public void Should_subscribe_to_FanSpeed_from_Cloud()
            {
                _sut.Run();
                
                _schedulerProvider.AdvanceTo(1);
                _cloudServiceMock.Verify(m => m.SetFan());
            }

            [Fact]
            public void Should_set_FanSpeed_from_cloud()
            {
                var fanMsg = new Fan {Number = 0, Speed = 40};

                _sut.Run();

                _schedulerProvider.AdvanceTo(2);
                _fanSubject.OnNext(fanMsg);

                _deviceServiceMock.Verify(m => m.SetFan(fanMsg));
            }
            
            [Fact(Skip = "Don't know how to test resubscription with subject")]
            public void Should_resubscribe_in_30_seconds_when_cloud_set_fan_throws_exception()
            {
                var fanMsg = new Fan {Number = 0, Speed = 40};
                
                _sut.Run();

                _schedulerProvider.AdvanceTo(2);
                _fanSubject.OnError(new Exception());
                
                _fanSubject.OnNext(fanMsg);
                _schedulerProvider.AdvanceBy(TimeSpan.FromSeconds(30).Ticks);
                _fanSubject.OnNext(fanMsg);
                
                _deviceServiceMock.Verify(m => m.SetFan(fanMsg));
            }
            
            [Fact]
            public void Should_resubscribe_in_30_seconds_when_device_set_fan_throws_exception()
            {
                var failingFanMsg = new Fan {Number = 0, Speed = 40};
                var successfullFanMsg = new Fan {Number = 0, Speed = 50};
                //First message will fail
                _deviceServiceMock.Setup(m => m.SetFan(It.IsAny<Fan>()))
                    .ThrowsAsync(new Exception());
                
                _sut.Run();

                _schedulerProvider.AdvanceTo(2);
                _fanSubject.OnNext(failingFanMsg);
                
                //Next message will not fail
                _deviceServiceMock.Setup(m => m.SetFan(It.IsAny<Fan>()))
                    .ReturnsAsync(true);
                
                //Advance to resubscription
                _schedulerProvider.AdvanceBy(TimeSpan.FromSeconds(30).Ticks);
                _fanSubject.OnNext(successfullFanMsg);
                
                _deviceServiceMock.Verify(m => m.SetFan(failingFanMsg));
                _deviceServiceMock.Verify(m => m.SetFan(successfullFanMsg));
            }
        }

        public class SetRelay : RunnerTests
        {
            [Fact]
            public void Should_subscribe_to_SetRelay_from_Cloud()
            {
                _sut.Run();
                
                _schedulerProvider.AdvanceTo(1);
                _cloudServiceMock.Verify(m => m.SetRelay());
            }

            [Fact]
            public void Should_set_Releayd_from_cloud()
            {
                var relayMsg = new Relay {Number = 1, On = true};

                _sut.Run();

                _schedulerProvider.AdvanceTo(2);
                _relaySubject.OnNext(relayMsg);

                _deviceServiceMock.Verify(m => m.SetRelay(relayMsg));
            }
            
            [Fact(Skip = "Don't know how to test resubscription with subject")]
            public void Should_resubscribe_in_30_seconds_when_cloud_set_relay_throws_exception()
            {
                var relayMsg = new Relay {Number = 1, On = true};
                
                _sut.Run();

                _schedulerProvider.AdvanceTo(2);
                _relaySubject.OnError(new Exception());
                
                _relaySubject.OnNext(relayMsg);
                _schedulerProvider.AdvanceBy(TimeSpan.FromSeconds(30).Ticks);
                _relaySubject.OnNext(relayMsg);
                
                _deviceServiceMock.Verify(m => m.SetRelay(relayMsg));
            }
            
            [Fact]
            public void Should_resubscribe_in_30_seconds_when_device_set_relay_throws_exception()
            {
                var failingRelayMSg = new Relay {Number = 1, On = false};
                var successfullRelayMsg = new Relay {Number = 0, On = true};
                //First message will fail
                _deviceServiceMock.Setup(m => m.SetFan(It.IsAny<Fan>()))
                    .ThrowsAsync(new Exception());
                
                _sut.Run();

                _schedulerProvider.AdvanceTo(2);
                _relaySubject.OnNext(failingRelayMSg);
                
                //Next message will not fail
                _deviceServiceMock.Setup(m => m.SetFan(It.IsAny<Fan>()))
                    .ReturnsAsync(true);
                
                //Advance to resubscription
                _schedulerProvider.AdvanceBy(TimeSpan.FromSeconds(30).Ticks);
                _relaySubject.OnNext(successfullRelayMsg);
                
                _deviceServiceMock.Verify(m => m.SetRelay(failingRelayMSg));
                _deviceServiceMock.Verify(m => m.SetRelay(successfullRelayMsg));
            }
        }
    }
}