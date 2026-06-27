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
        void ReceiveResult(InferenceResult result, Texture sourceTexture);
        void Clear();
        void Release();
    }
}
