using System;
using UnityEngine;

namespace UniversalTracker.Core
{
    public interface IVisionFrameSource : IDisposable
    {
        bool IsReady { get; }
        Vector2Int SourceSize { get; }
        VisionFrameSourceType SourceType { get; }

        void Initialize();
        bool TryGetFrame(out VisionFrame frame);
    }

    public interface IVisionRuntimeAdapter : IDisposable
    {
        VisionRuntimeKind RuntimeKind { get; }
        VisionModelCapability Capabilities { get; }
        bool IsInitialized { get; }

        void Initialize(VisionModelProfile profile);
        VisionFrameResult ProcessFrame(VisionFrame frame);
    }

    public interface IVisionFrameResultReceiver
    {
        void ReceiveVisionResult(VisionFrameResult result, Texture sourceTexture = null);
    }

    public interface IVisionModelAdapter
    {
        string AdapterId { get; }
        VisionModelFamily Family { get; }
        VisionModelCapability Capabilities { get; }

        bool CanHandle(VisionModelProfile profile);
        IVisionRuntimeAdapter CreateRuntime(VisionModelProfile profile);
    }

    public sealed class VisionPipelineContext
    {
        public VisionModelProfile ActiveModelProfile { get; private set; }
        public IVisionFrameSource FrameSource { get; private set; }
        public IVisionRuntimeAdapter Runtime { get; private set; }
        public VisionHealthState HealthState { get; private set; } = VisionHealthState.NotInitialized;
        public VisionError LastError { get; private set; }
        public VisionHealthStatus HealthStatus { get; private set; } =
            VisionHealthStatus.Create(VisionHealthState.NotInitialized, VisionHealthState.NotInitialized, VisionHealthEvent.None, "Pipeline is not initialized.");

        public void Bind(VisionModelProfile profile, IVisionFrameSource source, IVisionRuntimeAdapter runtime)
        {
            ActiveModelProfile = profile;
            FrameSource = source;
            Runtime = runtime;
            SetHealth(VisionHealthState.Initializing, VisionHealthEvent.None, "Pipeline is initializing.");
        }

        public VisionHealthStatus MarkRunning(string message = "Pipeline is running.")
        {
            VisionHealthEvent eventType =
                HealthState == VisionHealthState.Degraded || HealthState == VisionHealthState.Recovering
                    ? VisionHealthEvent.Recovered
                    : HealthState == VisionHealthState.Running
                        ? VisionHealthEvent.None
                        : VisionHealthEvent.Started;

            return SetHealth(VisionHealthState.Running, eventType, message);
        }

        public VisionHealthStatus MarkStopped(string message = "Pipeline is stopped.")
        {
            return SetHealth(VisionHealthState.Stopped, VisionHealthEvent.Stopped, message);
        }

        public VisionHealthStatus MarkDegraded(VisionError error, int consecutiveErrors, string message = "Pipeline is degraded.")
        {
            return SetHealth(VisionHealthState.Degraded, VisionHealthEvent.Degraded, message, error, consecutiveErrors);
        }

        public VisionHealthStatus MarkFailed(VisionError error, string message = "Pipeline failed.")
        {
            return SetHealth(VisionHealthState.Failed, VisionHealthEvent.Failed, message, error);
        }

        private VisionHealthStatus SetHealth(
            VisionHealthState state,
            VisionHealthEvent eventType,
            string message,
            VisionError error = null,
            int consecutiveRecoverableErrors = 0)
        {
            VisionHealthState previous = HealthState;
            HealthState = state;
            LastError = error;
            HealthStatus = VisionHealthStatus.Create(
                state,
                previous,
                eventType,
                message,
                error,
                consecutiveRecoverableErrors,
                UnityEngine.Time.realtimeSinceStartupAsDouble);
            return HealthStatus;
        }
    }
}
