using UnityEngine;
using UnityEngine.Events;
using UniversalTracker.Core;

namespace UniversalTracker.OutputReceivers
{
    public class EventOutputReceiver : MonoBehaviour, IOutputReceiver, IVisionFrameResultReceiver
    {
        [Header("Events")]
        [Tooltip("UnityEvent invoked for every processed VisionFrameResult.")]
        public UnityEvent<VisionFrameResult> OnVisionFrameReceived = new UnityEvent<VisionFrameResult>();

        [SerializeField, Tooltip("Enable this receiver.")]
        private bool isEnabled = true;
        public bool IsEnabled { get => isEnabled; set => isEnabled = value; }

        public void Initialize()
        {
        }

        public void ReceiveVisionResult(VisionFrameResult result, Texture sourceTexture = null)
        {
            if (!isEnabled || result == null)
                return;

            OnVisionFrameReceived?.Invoke(result);
        }

        public void Clear()
        {
        }

        public void Release()
        {
            OnVisionFrameReceived?.RemoveAllListeners();
        }
    }
}
