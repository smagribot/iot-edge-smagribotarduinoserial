using System;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using IoTEdgeSmagribotArduinoSerial.Services.Cloud;
using IoTEdgeSmagribotArduinoSerial.Services.Device;
using IoTEdgeSmagribotArduinoSerial.Services.DeviceCommunication;
using IoTEdgeSmagribotArduinoSerial.Services.Parser;
using IoTEdgeSmagribotArduinoSerial.Services.Scheduler;
using Microsoft.Extensions.Logging;

namespace IoTEdgeSmagribotArduinoSerial
{
    internal class Program
    {
        public static IContainer Container { get; set; }

        private static void Main(string[] args)
        {
            Console.WriteLine("                                                                                           \n" +
                              "                                                          ,,   ,,                          \n" +
                              " .M\"\"\"bgd                                                 db  *MM                    mm    \n" +
                              ",MI    \"Y                                                      MM                    MM    \n" +
                              "`MMb.     `7MMpMMMb.pMMMb.   ,6\"Yb.   .P\"Ybmmm `7Mb,od8 `7MM   MM,dMMb.   ,pW\"Wq.  mmMMmm  \n" +
                              "  `YMMNq.   MM    MM    MM  8)   MM  :MI  I8     MM' \"'   MM   MM    `Mb 6W'   `Wb   MM    \n" +
                              ".     `MM   MM    MM    MM   ,pm9MM   WmmmP\"     MM       MM   MM     M8 8M     M8   MM    \n" +
                              "Mb     dM   MM    MM    MM  8M   MM  8M          MM       MM   MM.   ,M9 YA.   ,A9   MM    \n" +
                              "P\"Ybmmd\"  .JMML  JMML  JMML.`Moo9^Yo. YMMMMMb  .JMML.   .JMML. P^YbmdP'   `Ybmd9'    `Mbmo \n" +
                              "                                     6'     dP                                             \n" +
                              "                                     Ybmmmd'                                                ");
            Console.WriteLine("Smagribot 🌱 Azure IoT Edge Arduino Serial Module");
            Console.WriteLine($"Version: {typeof(Program).Assembly.GetName().Version}");

            SetupIoC();

            var runner = Container.Resolve<Runner>();
            runner.Run();
            
            // Wait until the app unloads or is cancelled
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
            WhenCancelled(cts.Token).Wait();
            runner.Dispose();
        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        private static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }

        private static void SetupIoC()
        {
            var builder = new ContainerBuilder();

            var loggerFactory = LoggerFactory.Create(loggerBuilder =>
            {
                var logLevelEnv = Environment.GetEnvironmentVariable("LogLevel");

#if DEBUG
                var defaultLogLevel = LogLevel.Debug;
#else
                var defaultLogLevel = LogLevel.Information;
#endif
                switch (logLevelEnv?.ToLower())
                {
                    case "none":
                        defaultLogLevel = LogLevel.None;
                        break;
                    case "debug":
                        defaultLogLevel = LogLevel.Debug;
                        break;
                    case "information":
                        defaultLogLevel = LogLevel.Information;
                        break;
                }

                loggerBuilder
                    .SetMinimumLevel(defaultLogLevel)
                    .AddSimpleConsole(c =>
                    {
                        c.IncludeScopes = true;
                        c.SingleLine = true;
                        c.TimestampFormat = "[yyyy-MM-ddTHH:mm:ssZ] ";
                        c.UseUtcTimestamp = true;
                    })
                    .AddDebug();
            });

            var logger = loggerFactory.CreateLogger("Arduino Serial Module");
            builder.RegisterInstance(logger)
                .As<ILogger>()
                .SingleInstance();

            builder.RegisterType<SchedulerProvider>()
                .As<ISchedulerProvider>()
                .SingleInstance();

            var serialPortName = Environment.GetEnvironmentVariable("SerialPortName");
            if (serialPortName == null)
            {
                logger.LogWarning("No \"SerialPortName\" defined in enviroment variables! Settings default");
                serialPortName = "/dev/ttyACM0";
            }
            var serialBaudRate = int.Parse(Environment.GetEnvironmentVariable("SerialBaudRate") ?? "9600");
            builder.RegisterType<SerialCommunicationService>()
                .WithParameter("portName", serialPortName)
                .WithParameter("baudRate", serialBaudRate)
                .As<ICommunicationService>()
                .SingleInstance();

            builder.RegisterType<AzureIoTEdgeService>()
                .As<ICloudService>()
                .SingleInstance();

            builder.RegisterType<SmagriBotDevice>()
                .As<IDeviceService>()
                .SingleInstance();

            builder.RegisterType<SerialDeviceResultParser>()
                .As<IDeviceResultParser>()
                .SingleInstance();

            builder.RegisterType<Runner>()
                .SingleInstance();

            Container = builder.Build();
        }
    }
}
