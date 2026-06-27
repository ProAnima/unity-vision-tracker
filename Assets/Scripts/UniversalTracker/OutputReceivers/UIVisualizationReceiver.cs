using UnityEngine;
using UnityEngine.UI;
using UniversalTracker.Core;

namespace UniversalTracker.OutputReceivers
{
    public class UIVisualizationReceiver : MonoBehaviour, IOutputReceiver
    {
        public RawImage displayImage;
        [SerializeField] private bool isEnabled = true;

        public bool IsEnabled { get => isEnabled; set => isEnabled = value; }
        public VisionFrameResult LastResult { get; private set; }

        public void Initialize()
        {
            if (displayImage == null)
                displayImage = GetComponent<RawImage>();

            if (displayImage == null)
                displayImage = FindFirstObjectByType<RawImage>();
        }

        public void ReceiveVisionResult(VisionFrameResult result, Texture sourceTexture = null)
        {
            if (!isEnabled || result == null)
                return;

            LastResult = result;
            if (displayImage != null && sourceTexture != null)
                displayImage.texture = sourceTexture;
        }

        public void Clear()
        {
            LastResult = null;
        }

        public void Release()
        {
            LastResult = null;
        }
    }
}
