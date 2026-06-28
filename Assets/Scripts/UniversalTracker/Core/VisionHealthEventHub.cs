using System;

namespace UniversalTracker.Core
{
    internal sealed class VisionHealthEventHub
    {
        public VisionHealthStatus Status { get; private set; } =
            VisionHealthStatus.Create(VisionHealthState.NotInitialized, VisionHealthState.NotInitialized, VisionHealthEvent.None, "Tracker is not initialized.");

        public VisionHealthState State => Status?.state ?? VisionHealthState.NotInitialized;
        public VisionError LastError => Status?.lastError;

        public event Action<VisionHealthStatus> Changed;
        public event Action<VisionHealthStatus> Started;
        public event Action<VisionHealthStatus> Stopped;
        public event Action<VisionHealthStatus> Degraded;
        public event Action<VisionHealthStatus> Failed;
        public event Action<VisionHealthStatus> Recovered;

        public void Emit(VisionHealthStatus status)
        {
            if (status == null)
                return;

            Status = status;
            Changed?.Invoke(status);

            switch (status.eventType)
            {
                case VisionHealthEvent.Started:
                    Started?.Invoke(status);
                    break;
                case VisionHealthEvent.Stopped:
                    Stopped?.Invoke(status);
                    break;
                case VisionHealthEvent.Degraded:
                    Degraded?.Invoke(status);
                    break;
                case VisionHealthEvent.Failed:
                    Failed?.Invoke(status);
                    break;
                case VisionHealthEvent.Recovered:
                    Recovered?.Invoke(status);
                    break;
            }
        }
    }
}
