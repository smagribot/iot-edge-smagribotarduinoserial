using System.Reactive.Concurrency;

namespace IoTEdgeSmagribotArduinoSerial.Services.Scheduler
{
    public interface ISchedulerProvider
    {
        IScheduler NewThread { get; }
        IScheduler TaskPool { get; }
        IScheduler ThreadPool { get; }
    }
    
    public class SchedulerProvider : ISchedulerProvider
    {   
        public IScheduler NewThread => NewThreadScheduler.Default;
        public IScheduler TaskPool => TaskPoolScheduler.Default;
        public IScheduler ThreadPool => ThreadPoolScheduler.Instance;
    }
}