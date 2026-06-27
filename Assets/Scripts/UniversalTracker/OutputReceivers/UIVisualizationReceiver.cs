using UnityEngine;
using UnityEngine.UI;
using UniversalTracker.Core;
using UniversalTracker.Visualization;

namespace UniversalTracker.OutputReceivers
{
    /// <summary>
    /// Приёмник результатов с визуализацией через OnGUI (просто и надёжно)
    /// </summary>
    public class UIVisualizationReceiver : MonoBehaviour, IOutputReceiver
    {
        #region Public Fields
        
        [Header("🎨 UI элементы")]
        public RawImage displayImage;
        
        [Header("⚙️ Настройки визуализации")]
        [SerializeField] private bool isEnabled = true;
        public bool drawBoundingBoxes = true;
        public bool drawKeypoints = true;
        public bool drawConnections = true;
        public bool drawMasks = false;
        public bool drawConfidence = true;
        public bool showDebugInfo = true;
        
        public bool IsEnabled { get => isEnabled; set => isEnabled = value; }
        
        #endregion
        
        #region Private Fields
        
        private SimpleGUIVisualizer guiVisualizer;
        
        #endregion
        
        #region Public Methods
        
        public void Initialize()
        {
            try
            {
                if (displayImage == null)
                    displayImage = GetComponent<RawImage>();
                
                if (displayImage == null)
                    displayImage = FindFirstObjectByType<RawImage>();
                
                if (displayImage == null)
                {
                    Debug.LogWarning("⚠️ [UIVisOutput] RawImage не найден (опционально)");
                }
                
                // Создаём SimpleGUIVisualizer
                guiVisualizer = gameObject.AddComponent<SimpleGUIVisualizer>();
                guiVisualizer.enabled = isEnabled;
                guiVisualizer.drawBoundingBoxes = drawBoundingBoxes;
                guiVisualizer.drawKeypoints = drawKeypoints;
                guiVisualizer.drawConnections = drawConnections;
                guiVisualizer.drawMasks = drawMasks;
                guiVisualizer.showDebugInfo = showDebugInfo;
                
                Debug.Log("✅ [UIVisOutput] SimpleGUI визуализатор готов (OnGUI режим)");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ [UIVisOutput] Ошибка инициализации: {e.Message}");
                isEnabled = false;
            }
        }
        
        public void ReceiveResult(InferenceResult result, Texture sourceTexture)
        {
            if (!isEnabled) return;
            if (result == null || sourceTexture == null) return;
            
            try
            {
                // Обновляем RawImage если есть
                if (displayImage != null)
                    displayImage.texture = sourceTexture;
                
                // Передаём данные в GUI visualizer
                if (guiVisualizer != null)
                    guiVisualizer.UpdateVisualization(result, sourceTexture);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ [UIVisOutput] Ошибка визуализации: {e.Message}");
            }
        }
        
        public void Clear()
        {
            // GUI visualizer очищается автоматически каждый кадр
        }
        
        public void Release()
        {
            if (guiVisualizer != null)
            {
                Destroy(guiVisualizer);
                guiVisualizer = null;
            }
        }
        
        #endregion
    }
}
