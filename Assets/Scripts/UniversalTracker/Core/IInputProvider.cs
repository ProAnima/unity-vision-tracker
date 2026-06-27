using UnityEngine;
using System;

namespace UniversalTracker.Core
{
    /// <summary>
    /// Интерфейс провайдера входных данных (текстур)
    /// </summary>
    public interface IInputProvider
    {
        bool IsReady { get; }
        Texture CurrentTexture { get; }
        Vector2Int Resolution { get; }
        
        void Initialize();
        void UpdateTexture();
        void Release();
        
        event Action OnTextureUpdated;
    }
}
