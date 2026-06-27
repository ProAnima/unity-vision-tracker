using UnityEngine;
using UnityEngine.Events;
using UniversalTracker.Core;

namespace UniversalTracker.OutputReceivers
{
    /// <summary>
    /// Приёмник результатов через Unity Events
    /// </summary>
    public class EventOutputReceiver : MonoBehaviour, IOutputReceiver, IVisionFrameResultReceiver
    {
        #region Public Fields
        
        [Header("📢 События")]
        public UnityEvent<VisionFrameResult> OnVisionFrameReceived = new UnityEvent<VisionFrameResult>();
        public UnityEvent<InferenceResult> OnInferenceComplete = new UnityEvent<InferenceResult>();
        public UnityEvent<BBoxData[]> OnDetectionsReceived = new UnityEvent<BBoxData[]>();
        public UnityEvent<KeypointData[]> OnKeypointsReceived = new UnityEvent<KeypointData[]>();
        public UnityEvent<MaskData[]> OnMasksReceived = new UnityEvent<MaskData[]>();
        
        [Header("⚙️ Настройки")]
        [SerializeField] private bool isEnabled = true;
        public bool IsEnabled { get => isEnabled; set => isEnabled = value; }
        
        #endregion
        
        #region Public Methods
        
        public void Initialize()
        {
            Debug.Log("✅ [EventOutput] Event receiver инициализирован");
        }
        
        public void ReceiveResult(InferenceResult result, Texture sourceTexture)
        {
            if (!isEnabled) return;

            OnVisionFrameReceived?.Invoke(VisionResultAdapter.FromInferenceResult(result, sourceTexture));
            
            OnInferenceComplete?.Invoke(result);
            
            if (result.boundingBoxes != null && result.boundingBoxes.Length > 0)
                OnDetectionsReceived?.Invoke(result.boundingBoxes);
            
            if (result.keypoints != null && result.keypoints.Length > 0)
                OnKeypointsReceived?.Invoke(result.keypoints);
            
            if (result.masks != null && result.masks.Length > 0)
                OnMasksReceived?.Invoke(result.masks);
        }

        public void ReceiveVisionResult(VisionFrameResult result, Texture sourceTexture = null)
        {
            if (!isEnabled || result == null)
                return;

            OnVisionFrameReceived?.Invoke(result);
        }
        
        public void Clear()
        {
            // События не требуют очистки
        }
        
        public void Release()
        {
            OnVisionFrameReceived?.RemoveAllListeners();
            OnInferenceComplete?.RemoveAllListeners();
            OnDetectionsReceived?.RemoveAllListeners();
            OnKeypointsReceived?.RemoveAllListeners();
            OnMasksReceived?.RemoveAllListeners();
        }
        
        #endregion
    }
}
