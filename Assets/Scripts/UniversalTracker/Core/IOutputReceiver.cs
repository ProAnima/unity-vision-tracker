using UnityEngine;

namespace UniversalTracker.Core
{
    /// <summary>
    /// Интерфейс приёмника результатов inference
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
