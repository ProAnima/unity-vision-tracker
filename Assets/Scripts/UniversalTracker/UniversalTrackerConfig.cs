using UnityEngine;
using UniversalTracker.Core;

namespace UniversalTracker
{
    /// <summary>
    /// ScriptableObject конфигурация для универсального трекера
    /// </summary>
    [CreateAssetMenu(fileName = "TrackerConfig", menuName = "Universal Tracker/Tracker Config", order = 1)]
    public class UniversalTrackerConfig : ScriptableObject
    {
        [Header("🧠 Модель")]
        public ModelConfig modelConfig;
        
        [Header("📥 Input")]
        public InputProviderType inputType = InputProviderType.WebCam;
        public int targetFPS = 30;
        
        [Header("🎨 Визуализация")]
        public bool showBoundingBoxes = true;
        public bool showKeypoints = true;
        public bool showMasks = false;
        public bool showLabels = true;
        public bool showConfidence = true;
        
        public Color bboxColor = Color.green;
        public Color keypointColor = Color.cyan;
        public Color lineColor = Color.yellow;
        
        [Header("🔄 Tracking")]
        public bool enableTracking = true;
        public TrackerType trackerType = TrackerType.IOU;
        [Range(0.1f, 0.9f)] public float iouThreshold = 0.3f;
        [Range(1, 30)] public int maxMissedFrames = 10;
        
        [Header("⚙️ Дополнительно")]
        public bool logPerformance = true;
        public bool autoStart = true;
    }
}
