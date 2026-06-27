using System;

namespace UniversalTracker.Core
{
    /// <summary>
    /// Model-agnostic runtime pipeline that processes frames through a source and runtime adapter.
    /// </summary>
    public sealed class VisionPipeline : IDisposable
    {
        public VisionPipelineContext Context { get; } = new VisionPipelineContext();
        public VisionFrameResult LastResult { get; private set; }
        public bool IsRunning { get; private set; }

        public event Action<VisionFrameResult> FrameProcessed;
        public event Action<VisionError> ErrorReceived;

        public void Configure(VisionModelProfile profile, IVisionFrameSource source, IVisionRuntimeAdapter runtime)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (runtime == null)
                throw new ArgumentNullException(nameof(runtime));

            Context.Bind(profile, source, runtime);
            LastResult = null;
            IsRunning = false;
        }

        public bool Start()
        {
            if (Context.FrameSource == null || Context.Runtime == null)
            {
                ReportFailure(VisionErrorCode.ModelNotConfigured, "VisionPipeline is not configured.", false);
                return false;
            }

            try
            {
                Context.FrameSource.Initialize();
                Context.Runtime.Initialize(Context.ActiveModelProfile);
            }
            catch (Exception e)
            {
                ReportFailure(VisionErrorCode.ModelInitializationFailed, "VisionPipeline failed to initialize.", false, e);
                return false;
            }

            if (!Context.Runtime.IsInitialized)
            {
                ReportFailure(VisionErrorCode.ModelInitializationFailed, "Runtime adapter did not initialize.", false);
                return false;
            }

            IsRunning = true;
            Context.MarkRunning();
            return true;
        }

        public bool TryProcessNext(out VisionFrameResult result)
        {
            result = null;

            if (!IsRunning)
                return false;

            if (Context.FrameSource == null || Context.Runtime == null)
            {
                ReportFailure(VisionErrorCode.ModelNotConfigured, "VisionPipeline lost its configured source or runtime.", false);
                return false;
            }

            if (!Context.FrameSource.IsReady)
            {
                ReportFailure(VisionErrorCode.SourceNotReady, "Frame source is not ready.", true);
                return false;
            }

            if (!Context.FrameSource.TryGetFrame(out VisionFrame frame) || !frame.IsValid)
            {
                ReportFailure(VisionErrorCode.InvalidFrame, "Frame source returned no valid frame.", true);
                return false;
            }

            try
            {
                result = Context.Runtime.ProcessFrame(frame);
            }
            catch (Exception e)
            {
                ReportFailure(VisionErrorCode.InferenceFailed, "Runtime adapter failed while processing a frame.", true, e);
                return false;
            }

            if (result == null)
            {
                ReportFailure(VisionErrorCode.PostprocessFailed, "Runtime adapter returned null result.", true);
                return false;
            }

            LastResult = result;
            Context.MarkRunning();
            FrameProcessed?.Invoke(result);
            return true;
        }

        public void Stop()
        {
            IsRunning = false;
            Context.MarkStopped();
        }

        public void Dispose()
        {
            Stop();
            Context.Runtime?.Dispose();
            Context.FrameSource?.Dispose();
        }

        private void ReportFailure(VisionErrorCode code, string message, bool recoverable, Exception exception = null)
        {
            var error = new VisionError(code, message, recoverable, exception);

            if (recoverable)
                Context.MarkRunning();
            else
                Context.MarkFailed(error);

            ErrorReceived?.Invoke(error);
        }
    }
}
