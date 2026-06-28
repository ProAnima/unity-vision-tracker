using UnityEngine;

namespace UniversalTracker.Core
{
    /// <summary>
    /// Receiver contract for production vision frame results.
    /// </summary>
    public interface IOutputReceiver
    {
        bool IsEnabled { get; set; }

        void Initialize();
        void ReceiveVisionResult(VisionFrameResult result, Texture sourceTexture = null);
        void Clear();
        void Release();
    }
}
