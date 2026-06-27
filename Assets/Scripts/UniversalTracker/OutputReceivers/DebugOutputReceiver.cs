using UnityEngine;
using UniversalTracker.Core;

namespace UniversalTracker.OutputReceivers
{
    /// <summary>
    /// Приёмник результатов для отладочного вывода
    /// </summary>
    public class DebugOutputReceiver : MonoBehaviour, IOutputReceiver
    {
        #region Public Fields
        
        [Header("🔍 Настройки отладки")]
        [SerializeField] private bool isEnabled = true;
        public bool logToConsole = true;
        public bool showDebugWindow = false;
        
        [Range(0.1f, 5f)] public float logInterval = 1f;
        
        public bool IsEnabled { get => isEnabled; set => isEnabled = value; }
        
        #endregion
        
        #region Public Methods
        
        public void Initialize()
        {
            lastLogTime = Time.time;
            Debug.Log("✅ [DebugOutput] Debug receiver готов");
        }
        
        public void ReceiveResult(InferenceResult result, Texture sourceTexture)
        {
            if (!isEnabled) return;
            
            if (logToConsole && Time.time - lastLogTime >= logInterval)
            {
                LogResult(result);
                lastLogTime = Time.time;
            }
            
            lastResult = result;
        }
        
        public void Clear()
        {
            lastResult = null;
        }
        
        public void Release()
        {
            lastResult = null;
        }
        
        #endregion
        
        #region Private Fields
        
        private float lastLogTime;
        private InferenceResult lastResult;
        
        #endregion
        
        #region Private Methods
        
        private void LogResult(InferenceResult result)
        {
            if (!result.success)
            {
                Debug.LogWarning("⚠️ [Debug] Inference не удался");
                return;
            }
            
            var msg = $"📊 [Debug] Inference: {result.inferenceTime:F2}ms | ";
            msg += $"Детекции: {result.detectionCount} | ";
            msg += $"Max confidence: {result.maxConfidence:F2}";
            
            Debug.Log(msg);
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        void OnGUI()
        {
            if (!showDebugWindow || lastResult == null) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Box("🔍 Debug Info");
            
            GUILayout.Label($"Model: {lastResult.modelType}");
            GUILayout.Label($"Inference: {lastResult.inferenceTime:F2}ms");
            GUILayout.Label($"Detections: {lastResult.detectionCount}");
            GUILayout.Label($"Max Confidence: {lastResult.maxConfidence:F2}");
            
            GUILayout.EndArea();
        }
        
        #endregion
    }
}
