using UnityEngine;
using UniversalTracker.Core;

namespace UniversalTracker.OutputReceivers
{
    /// <summary>
    /// Приёмник результатов с визуализацией в 3D сцене
    /// </summary>
    public class SceneVisualizationReceiver : MonoBehaviour, IOutputReceiver
    {
        #region Public Fields
        
        [Header("🎯 Настройки сцены")]
        public Camera targetCamera;
        public Transform visualizationRoot;
        
        [Header("⚙️ Визуализация")]
        [SerializeField] private bool isEnabled = true;
        public bool spawn3DMarkers = true;
        public GameObject bboxMarkerPrefab;
        public GameObject keypointMarkerPrefab;
        
        public bool IsEnabled { get => isEnabled; set => isEnabled = value; }
        
        #endregion
        
        #region Public Methods
        
        public void Initialize()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;
            
            if (visualizationRoot == null)
            {
                var rootObj = new GameObject("SceneVisualization");
                rootObj.transform.SetParent(transform);
                visualizationRoot = rootObj.transform;
            }
            
            Debug.Log("✅ [SceneVisOutput] Визуализатор сцены готов");
        }
        
        public void ReceiveResult(InferenceResult result, Texture sourceTexture)
        {
            if (!isEnabled) return;
            
            Clear();
            
            if (spawn3DMarkers && result.boundingBoxes != null)
                SpawnBBoxMarkers(result.boundingBoxes);
            
            if (spawn3DMarkers && result.keypoints != null)
                SpawnKeypointMarkers(result.keypoints);
        }
        
        public void Clear()
        {
            if (visualizationRoot == null) return;
            
            foreach (Transform child in visualizationRoot)
                Destroy(child.gameObject);
        }
        
        public void Release()
        {
            Clear();
        }
        
        #endregion
        
        #region Private Methods
        
        private void SpawnBBoxMarkers(BBoxData[] boxes)
        {
            if (bboxMarkerPrefab == null) return;
            
            foreach (var box in boxes)
            {
                var marker = Instantiate(bboxMarkerPrefab, visualizationRoot);
                // Позиционирование будет добавлено
            }
        }
        
        private void SpawnKeypointMarkers(KeypointData[] keypoints)
        {
            if (keypointMarkerPrefab == null) return;
            
            foreach (var kp in keypoints)
            {
                if (kp.points == null) continue;
                
                foreach (var point in kp.points)
                {
                    var marker = Instantiate(keypointMarkerPrefab, visualizationRoot);
                    // Позиционирование будет добавлено
                }
            }
        }
        
        #endregion
    }
}
