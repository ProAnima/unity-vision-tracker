using UnityEngine;
using System;
using UniversalTracker.Core;

namespace UniversalTracker.InputProviders
{
    /// <summary>
    /// Провайдер входных данных из готовой текстуры
    /// </summary>
    public class TextureInputProvider : MonoBehaviour, IInputProvider
    {
        #region Public Fields
        
        [Header("🖼️ Текстура")]
        public Texture sourceTexture;
        
        public bool IsReady => sourceTexture != null;
        public Texture CurrentTexture => sourceTexture;
        public Vector2Int Resolution => sourceTexture != null ? new Vector2Int(sourceTexture.width, sourceTexture.height) : Vector2Int.zero;
        
        public event Action OnTextureUpdated;
        
        #endregion
        
        #region Public Methods
        
        public void Initialize()
        {
            if (sourceTexture == null)
                Debug.LogWarning("⚠️ [TextureInput] Текстура не назначена!");
            else
                Debug.Log($"✅ [TextureInput] Текстура готова: {sourceTexture.width}x{sourceTexture.height}");
        }
        
        public void UpdateTexture()
        {
            if (IsReady)
                OnTextureUpdated?.Invoke();
        }
        
        public void Release()
        {
            sourceTexture = null;
        }
        
        public void SetTexture(Texture texture)
        {
            sourceTexture = texture;
            OnTextureUpdated?.Invoke();
        }
        
        #endregion
    }
}
