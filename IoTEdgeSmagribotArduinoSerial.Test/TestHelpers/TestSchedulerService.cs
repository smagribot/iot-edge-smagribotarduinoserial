using System.Reactive.Concurrency;
using IoTEdgeSmagribotArduinoSerial.Services.Scheduler;
using Microsoft.Reactive.Testing;

namespace IoTEdgeSmagribotArduinoSerial.Test.TestHelpers
{
    public class TestSchedulerProvider : TestScheduler, ISchedulerProvider
    {
        public IScheduler NewThread { get; }
        public IScheduler TaskPool { get; }
        public IScheduler ThreadPool { get; }

        public TestSchedulerProvider()
        {
            NewThread = this;
            TaskPool = this;
            ThreadPool = this;
        }
    }
}