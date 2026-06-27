using UnityEngine;
using System;
using UniversalTracker.Core;

namespace UniversalTracker.InputProviders
{
    /// <summary>
    /// Провайдер входных данных из Unity Camera через RenderTexture
    /// </summary>
    public class CameraInputProvider : MonoBehaviour, IInputProvider
    {
        #region Public Fields
        
        [Header("📷 Настройки камеры")]
        public Camera sourceCamera;
        public int textureWidth = 1920;
        public int textureHeight = 1080;
        public RenderTextureFormat format = RenderTextureFormat.ARGB32;
        
        public bool IsReady => renderTexture != null && sourceCamera != null;
        public Texture CurrentTexture => renderTexture;
        public Vector2Int Resolution => renderTexture != null ? new Vector2Int(renderTexture.width, renderTexture.height) : Vector2Int.zero;
        
        public event Action OnTextureUpdated;
        
        #endregion
        
        #region Public Methods
        
        public void Initialize()
        {
            if (sourceCamera == null)
                sourceCamera = Camera.main;
            
            if (sourceCamera == null)
            {
                Debug.LogError("❌ [CameraInput] Камера не найдена!");
                return;
            }
            
            renderTexture = new RenderTexture(textureWidth, textureHeight, 24, format);
            renderTexture.Create();
            
            sourceCamera.targetTexture = renderTexture;
            
            Debug.Log($"✅ [CameraInput] RenderTexture создана: {textureWidth}x{textureHeight}");
        }
        
        public void UpdateTexture()
        {
            if (IsReady)
                OnTextureUpdated?.Invoke();
        }
        
        public void Release()
        {
            if (sourceCamera != null)
                sourceCamera.targetTexture = null;
            
            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
                renderTexture = null;
                Debug.Log("🔌 [CameraInput] RenderTexture освобождена");
            }
        }
        
        #endregion
        
        #region Private Fields
        
        private RenderTexture renderTexture;
        
        #endregion
        
        #region Unity Lifecycle
        
        void OnDestroy() => Release();
        
        #endregion
    }
}
