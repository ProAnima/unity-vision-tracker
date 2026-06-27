using System;

namespace UniversalTracker.Core
{
    /// <summary>
    /// High-level task category exposed by the production vision API.
    /// </summary>
    public enum VisionTaskType
    {
        Detection,
        HumanDetection,
        Pose,
        Segmentation,
        Classification,
        Custom
    }

    /// <summary>
    /// Source type for frames entering the vision pipeline.
    /// </summary>
    public enum VisionFrameSourceType
    {
        Unknown,
        WebCam,
        UnityCamera,
        RenderTexture,
        Texture,
        Video,
        ARFoundation,
        XRPassThrough,
        Custom
    }

    /// <summary>
    /// Orientation metadata for camera frames before preprocessing.
    /// </summary>
    public enum VisionFrameOrientation
    {
        Rotation0,
        Rotation90,
        Rotation180,
        Rotation270
    }

    /// <summary>
    /// Lifecycle state assigned by tracking.
    /// </summary>
    public enum VisionTrackState
    {
        None,
        New,
        Tracking,
        Lost,
        Removed
    }

    /// <summary>
    /// Runtime health state for the production pipeline.
    /// </summary>
    public enum VisionHealthState
    {
        NotInitialized,
        Initializing,
        Running,
        Degraded,
        Recovering,
        Stopped,
        Failed
    }

    /// <summary>
    /// Structured error code used by manager/runtime layers.
    /// </summary>
    public enum VisionErrorCode
    {
        Unknown,
        SourceNotReady,
        InvalidFrame,
        ModelNotConfigured,
        ModelInitializationFailed,
        PreprocessFailed,
        InferenceFailed,
        PostprocessFailed,
        TrackingFailed,
        ReceiverFailed
    }

    [Serializable]
    public sealed class VisionError
    {
        public VisionErrorCode code;
        public string message;
        public string exceptionType;
        public bool isRecoverable;

        public VisionError(VisionErrorCode code, string message, bool isRecoverable = true, Exception exception = null)
        {
            this.code = code;
            this.message = message;
            this.isRecoverable = isRecoverable;
            exceptionType = exception?.GetType().Name;
        }
    }
}

