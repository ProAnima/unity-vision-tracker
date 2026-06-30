using UnityEngine;

namespace UniversalTracker
{
    [CreateAssetMenu(fileName = "SafetyConfig", menuName = "Universal Tracker/Safety Config")]
    public class SafetyConfig : ScriptableObject
    {
        [Header("Runtime Limits")]
        [Tooltip("Maximum expected inference time in milliseconds before runtime policy can treat the frame as slow.")]
        public float maxInferenceTimeMs = 1000f;

        [Tooltip("Allow frame skipping when inference takes longer than the configured budget.")]
        public bool skipFramesOnSlowInference = true;

        [Tooltip("Automatically restart after non-recoverable runtime failures when a supervisor implements restart policy.")]
        public bool autoRestartOnCriticalError;

        [Tooltip("Maximum consecutive recoverable errors before the manager stops the runtime.")]
        public int maxConsecutiveErrors = 10;

        [Header("Debug")]
        [Tooltip("Log additional diagnostic warnings and state transitions.")]
        public bool verboseLogging;
        [Tooltip("Log every processed frame. This is noisy and should stay disabled in production.")]
        public bool logEveryFrame;
        [Tooltip("Stop immediately on the first runtime error.")]
        public bool stopOnFirstError;
    }
}
