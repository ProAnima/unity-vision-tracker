using UnityEngine;
using UniversalTracker.Core;

namespace UniversalTracker
{
    /// <summary>
    /// Preset asset for the production tracker facade.
    /// </summary>
    [CreateAssetMenu(fileName = "TrackerConfig", menuName = "Universal Tracker/Tracker Config", order = 1)]
    public class UniversalTrackerConfig : ScriptableObject
    {
        [Header("Pipeline")]
        [Tooltip("Pipeline profile used by the tracker.")]
        public VisionPipelineProfile pipelineProfile;
        [Tooltip("Compatibility single-model profile used when no pipeline profile is assigned.")]
        public VisionModelProfile modelProfile;

        [Header("Input")]
        [Tooltip("Default input source type.")]
        public InputProviderType inputType = InputProviderType.WebCam;
        [Tooltip("Default target processing frame rate.")]
        public int targetFPS = 30;

        [Header("Visualization")]
        [Tooltip("Show detection bounding boxes.")]
        public bool showBoundingBoxes = true;
        [Tooltip("Show pose keypoints.")]
        public bool showKeypoints = true;
        [Tooltip("Show segmentation masks.")]
        public bool showMasks = false;
        [Tooltip("Show class labels.")]
        public bool showLabels = true;
        [Tooltip("Show confidence values in labels.")]
        public bool showConfidence = true;

        [Tooltip("Default bounding box color.")]
        public Color bboxColor = Color.green;
        [Tooltip("Default keypoint color.")]
        public Color keypointColor = Color.cyan;
        [Tooltip("Default skeleton line color.")]
        public Color lineColor = Color.yellow;

        [Header("Tracking")]
        [Tooltip("Enable tracking by default.")]
        public bool enableTracking = true;
        [Tooltip("Default tracking algorithm.")]
        public TrackerType trackerType = TrackerType.IOU;
        [Tooltip("Minimum overlap required to match a detection with an existing track.")]
        [Range(0.1f, 0.9f)] public float iouThreshold = 0.3f;
        [Tooltip("Frames a track can be missing before it is removed.")]
        [Range(1, 30)] public int maxMissedFrames = 10;

        [Header("Runtime")]
        [Tooltip("Log performance summaries during runtime.")]
        public bool logPerformance = true;
        [Tooltip("Start automatically when the configured tracker starts.")]
        public bool autoStart = true;
    }
}
