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

    /// <summary>
    /// Capabilities exposed by a model adapter or runtime plugin.
    /// </summary>
    [Flags]
    public enum VisionModelCapability
    {
        None = 0,
        Detection = 1 << 0,
        HumanDetection = 1 << 1,
        Pose2D = 1 << 2,
        Pose3D = 1 << 3,
        Segmentation = 1 << 4,
        Classification = 1 << 5,
        Depth = 1 << 6,
        OpenVocabulary = 1 << 7,
        Promptable = 1 << 8,
        TemporalMemory = 1 << 9,
        WorldProjection = 1 << 10
    }

    public enum VisionModelFamily
    {
        Unknown,
        YOLO,
        MediaPipe,
        SAM,
        Depth,
        Custom
    }

    public enum VisionModelSourceFormat
    {
        Unknown,
        ONNX,
        UnityModelAsset,
        NativePlugin,
        RemoteEndpoint,
        Custom
    }

    public enum VisionRuntimeKind
    {
        UnityInferenceEngine,
        MediaPipe,
        NativePlugin,
        Remote,
        Mock,
        Custom
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
