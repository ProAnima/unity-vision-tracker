using UnityEngine;
using System;
using UniversalTracker.Core;

namespace UniversalTracker.InputProviders
{
    /// <summary>
    /// Провайдер входных данных с веб-камеры
    /// </summary>
    public class WebCamInputProvider : MonoBehaviour, IInputProvider
    {
        #region Public Fields
        
        [Header("🎥 Выбор камеры")]
        [Tooltip("Индекс камеры (0 = первая, 1 = вторая, и т.д.)")]
        public int cameraIndex = 0;
        
        [Tooltip("Или укажите имя камеры напрямую (оставьте пустым для автовыбора по индексу)")]
        public string deviceName;
        
        [Header("🎥 Настройки качества")]
        [Tooltip("Разрешение RenderTexture для inference (YOLO требует 640x640!)")]
        public int requestedWidth = 640;
        public int requestedHeight = 640; // YOLO требует квадратное изображение!
        public int requestedFPS = 30;
        
        public bool IsReady => webCamTexture != null && webCamTexture.isPlaying && renderTexture != null && framesSinceStart > 3;
        public Texture CurrentTexture => renderTexture;
        public Vector2Int Resolution => renderTexture != null ? new Vector2Int(renderTexture.width, renderTexture.height) : Vector2Int.zero;
        
        public event Action OnTextureUpdated;
        
        #endregion
        
        #region Public Methods
        
        public void Initialize()
        {
            try
            {
                ListAvailableCameras();
                
                if (string.IsNullOrEmpty(deviceName))
                {
                    if (WebCamTexture.devices.Length == 0)
                    {
                        Debug.LogError("❌ [WebCam] Веб-камеры не найдены!");
                        return;
                    }
                    
                    if (cameraIndex >= WebCamTexture.devices.Length)
                    {
                        Debug.LogWarning($"⚠️ [WebCam] Индекс {cameraIndex} вне диапазона, используем камеру 0");
                        cameraIndex = 0;
                    }
                    
                    deviceName = WebCamTexture.devices[cameraIndex].name;
                    Debug.Log($"📷 [WebCam] Выбрана камера [{cameraIndex}]: {deviceName}");
                }
                
                // Создаём WebCamTexture (используем нативное разрешение камеры для лучшей производительности)
                webCamTexture = new WebCamTexture(deviceName);
                
                if (webCamTexture == null)
                {
                    Debug.LogError("❌ [WebCam] Не удалось создать WebCamTexture!");
                    return;
                }
                
                webCamTexture.Play();
                
                // КРИТИЧЕСКИ ВАЖНО: YOLO требует 640x640!
                // Используем именно этот размер независимо от requestedWidth/Height
                const int YOLO_SIZE = 640;
                int rtWidth = YOLO_SIZE;
                int rtHeight = YOLO_SIZE;
                
                if (requestedWidth != YOLO_SIZE || requestedHeight != YOLO_SIZE)
                {
                    Debug.LogWarning($"⚠️ [WebCam] Запрошенный размер {requestedWidth}x{requestedHeight} не соответствует требованиям YOLO!");
                    Debug.LogWarning($"   Используем YOLO размер: {rtWidth}x{rtHeight}");
                }
                
                // Создаём RenderTexture размера YOLO для inference
                // (Unity.InferenceEngine лучше работает с RenderTexture)
                renderTexture = new RenderTexture(rtWidth, rtHeight, 0, RenderTextureFormat.ARGB32)
                {
                    filterMode = FilterMode.Bilinear,
                    enableRandomWrite = false,
                    useMipMap = false
                };
                renderTexture.Create();
                
                Debug.Log($"   RenderTexture создан: {renderTexture.width}x{renderTexture.height}, format: {renderTexture.format}");
                
                // Ждём пока камера действительно запустится
                framesSinceStart = 0;
                
                Debug.Log($"✅ [WebCam] Камера запущена: {deviceName}");
                Debug.Log($"   Нативное разрешение камеры: {webCamTexture.width}x{webCamTexture.height} (будет доступно после первых кадров)");
                Debug.Log($"   RenderTexture для inference: {renderTexture.width}x{renderTexture.height}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ [WebCam] КРИТИЧЕСКАЯ ОШИБКА Initialize: {e.Message}\n{e.StackTrace}");
            }
        }
        
        public void UpdateTexture()
        {
            try
            {
                if (webCamTexture == null || !webCamTexture.isPlaying)
                {
                    if (framesSinceStart == 0)
                        Debug.LogWarning("⚠️ [WebCam] UpdateTexture: камера не играет");
                    return;
                }
                
                // КРИТИЧЕСКАЯ ПРОВЕРКА: размеры должны быть валидны
                if (webCamTexture.width <= 16 || webCamTexture.height <= 16)
                {
                    if (framesSinceStart <= 10)
                        Debug.LogWarning($"⚠️ [WebCam] UpdateTexture: размеры камеры ещё не готовы ({webCamTexture.width}x{webCamTexture.height})");
                    return;
                }
                
                framesSinceStart++;
                
                bool isFirstFrames = framesSinceStart <= 10;
                
                if (isFirstFrames)
                    Debug.Log($"🎥 [WebCam] UpdateTexture #{framesSinceStart}: камера {webCamTexture.width}x{webCamTexture.height}, didUpdate: {webCamTexture.didUpdateThisFrame}");
                
                // Копируем WebCamTexture → RenderTexture
                // На первых кадрах копируем ВСЕГДА, даже если didUpdate = false
                bool shouldBlit = webCamTexture.didUpdateThisFrame || framesSinceStart <= 15;
                
                if (renderTexture != null && shouldBlit)
                {
                    if (isFirstFrames)
                        Debug.Log($"   >>> Вызов Graphics.Blit() (didUpdate: {webCamTexture.didUpdateThisFrame}, forced: {!webCamTexture.didUpdateThisFrame}) <<<");
                    
                    Graphics.Blit(webCamTexture, renderTexture);
                    
                    if (isFirstFrames)
                        Debug.Log($"   >>> Graphics.Blit() завершился! <<<");
                    
                    if (isFirstFrames)
                        Debug.Log($"   ✅ Скопировано → RT {renderTexture.width}x{renderTexture.height}, IsReady: {IsReady}");
                    
                    if (OnTextureUpdated != null)
                        OnTextureUpdated.Invoke();
                }
                else if (isFirstFrames)
                {
                    Debug.LogWarning($"   ⚠️ Blit пропущен (didUpdate: {webCamTexture.didUpdateThisFrame}, frame: {framesSinceStart})");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"💥 [WebCam] КРАШ в UpdateTexture: {e.Message}");
                Debug.LogError($"   StackTrace: {e.StackTrace}");
                Debug.LogError($"   WebCam: {webCamTexture?.width}x{webCamTexture?.height}, playing: {webCamTexture?.isPlaying}");
                Debug.LogError($"   RenderTexture: {renderTexture?.width}x{renderTexture?.height}, created: {renderTexture?.IsCreated()}");
            }
        }
        
        /// <summary>
        /// Список всех доступных камер
        /// </summary>
        public void ListAvailableCameras()
        {
            var devices = WebCamTexture.devices;
            
            if (devices.Length == 0)
            {
                Debug.LogWarning("⚠️ [WebCam] Камеры не найдены");
                return;
            }
            
            Debug.Log($"📷 [WebCam] Найдено камер: {devices.Length}");
            for (int i = 0; i < devices.Length; i++)
            {
                var device = devices[i];
                Debug.Log($"   [{i}] {device.name} (Front: {device.isFrontFacing})");
            }
        }
        
        /// <summary>
        /// Переключить на другую камеру
        /// </summary>
        public void SwitchCamera(int newIndex)
        {
            if (newIndex < 0 || newIndex >= WebCamTexture.devices.Length)
            {
                Debug.LogError($"❌ [WebCam] Неверный индекс камеры: {newIndex}");
                return;
            }
            
            Release();
            cameraIndex = newIndex;
            Initialize();
        }
        
        public void Release()
        {
            if (webCamTexture != null)
            {
                webCamTexture.Stop();
                Destroy(webCamTexture);
                webCamTexture = null;
            }
            
            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
                renderTexture = null;
            }
            
            Debug.Log("🔌 [WebCam] Камера остановлена и ресурсы освобождены");
        }
        
        #endregion
        
        #region Private Fields
        
        private WebCamTexture webCamTexture;
        private RenderTexture renderTexture;
        private int framesSinceStart;
        
        #endregion
        
        #region Unity Lifecycle
        
        void OnDestroy() => Release();
        
        #endregion
    }
}
