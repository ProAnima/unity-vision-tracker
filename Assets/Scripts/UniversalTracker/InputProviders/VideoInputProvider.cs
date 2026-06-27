using UnityEngine;
using UnityEngine.Video;
using System;
using UniversalTracker.Core;

namespace UniversalTracker.InputProviders
{
    /// <summary>
    /// Провайдер входных данных из VideoPlayer
    /// </summary>
    public class VideoInputProvider : MonoBehaviour, IInputProvider
    {
        #region Public Fields
        
        [Header("🎬 Видео плеер")]
        public VideoPlayer videoPlayer;
        public bool autoPlay = true;
        
        public bool IsReady => videoPlayer != null && videoPlayer.texture != null;
        public Texture CurrentTexture => videoPlayer?.texture;
        public Vector2Int Resolution => IsReady ? new Vector2Int((int)videoPlayer.width, (int)videoPlayer.height) : Vector2Int.zero;
        
        public event Action OnTextureUpdated;
        
        #endregion
        
        #region Public Methods
        
        public void Initialize()
        {
            if (videoPlayer == null)
                videoPlayer = GetComponent<VideoPlayer>();
            
            if (videoPlayer == null)
            {
                Debug.LogError("❌ [VideoInput] VideoPlayer не найден!");
                return;
            }
            
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.Prepare();
        }
        
        public void UpdateTexture()
        {
            if (IsReady && videoPlayer.isPlaying)
                OnTextureUpdated?.Invoke();
        }
        
        public void Release()
        {
            if (videoPlayer != null)
            {
                videoPlayer.prepareCompleted -= OnVideoPrepared;
                videoPlayer.Stop();
                Debug.Log("🔌 [VideoInput] Видео остановлено");
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void OnVideoPrepared(VideoPlayer source)
        {
            Debug.Log($"✅ [VideoInput] Видео готово: {videoPlayer.width}x{videoPlayer.height}");
            
            if (autoPlay)
                videoPlayer.Play();
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        void OnDestroy() => Release();
        
        #endregion
    }
}
