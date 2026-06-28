using UnityEngine;
using UniversalTracker.Core;

namespace UniversalTracker.OutputReceivers
{
    public class DebugOutputReceiver : MonoBehaviour, IOutputReceiver
    {
        [SerializeField] private bool isEnabled = true;
        public bool logToConsole = true;
        public bool showDebugWindow = false;
        [Range(0.1f, 5f)] public float logInterval = 1f;

        public bool IsEnabled { get => isEnabled; set => isEnabled = value; }

        private float lastLogTime;
        private VisionFrameResult lastResult;

        public void Initialize()
        {
            lastLogTime = Time.time;
        }

        public void ReceiveVisionResult(VisionFrameResult result, Texture sourceTexture = null)
        {
            if (!isEnabled || result == null)
                return;

            lastResult = result;
            if (logToConsole && Time.time - lastLogTime >= logInterval)
            {
                Debug.Log($"[VisionDebug] frame={result.frameIndex} results={result.TotalResultCount} totalMs={result.stats.totalMs:F2}");
                lastLogTime = Time.time;
            }
        }

        public void Clear()
        {
            lastResult = null;
        }

        public void Release()
        {
            lastResult = null;
        }

        private void OnGUI()
        {
            if (!showDebugWindow || lastResult == null)
                return;

            GUILayout.BeginArea(new Rect(10, 10, 320, 170));
            GUILayout.Box("Vision Debug");
            GUILayout.Label($"Frame: {lastResult.frameIndex}");
            GUILayout.Label($"Results: {lastResult.TotalResultCount}");
            GUILayout.Label($"Detections: {lastResult.detections?.Length ?? 0}");
            GUILayout.Label($"Poses: {lastResult.poses?.Length ?? 0}");
            GUILayout.Label($"Masks: {lastResult.masks?.Length ?? 0}");
            GUILayout.Label($"Total: {lastResult.stats.totalMs:F2}ms");
            GUILayout.EndArea();
        }
    }
}
