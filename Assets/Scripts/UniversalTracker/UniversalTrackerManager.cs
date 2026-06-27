using UnityEngine;
using System;
using System.Collections.Generic;
using UniversalTracker.Core;
using UniversalTracker.InputProviders;
using UniversalTracker.OutputReceivers;
using UniversalTracker.Models;
using UniversalTracker.Tracking;
using static UniversalTracker.Models.YOLOModelFactory;

namespace UniversalTracker
{
    /// <summary>
    /// Главный менеджер универсальной системы трекинга
    /// </summary>
    public class UniversalTrackerManager : MonoBehaviour
    {
        #region Public Fields
        
        [Header("🎯 Основные настройки")]
        public bool autoStart = true;
        [Range(1, 60)] public int targetFPS = 30;
        public bool useTracking = true;
        
        [Header("📥 Input Provider")]
        public InputProviderType inputType = InputProviderType.WebCam;
        public MonoBehaviour customInputProvider;
        
        [Header("🧠 Модели")]
        public ModelConfig[] modelConfigs;
        public int activeModelIndex = 0;
        
        [Header("🎨 Output Receivers")]
        [Tooltip("Использовать готовые компоненты со сцены (если привязаны) или создавать динамически")]
        public UIVisualizationReceiver manualUIReceiver;
        public VisionToolkitDashboardReceiver manualToolkitDashboardReceiver;
        public EventOutputReceiver manualEventReceiver;
        public SceneVisualizationReceiver manualSceneReceiver;
        public DebugOutputReceiver manualDebugReceiver;
        
        [Space(5)]
        [Tooltip("Создавать receivers динамически если не привязаны вручную")]
        public bool useEventOutput = true;
        public bool useUIVisualization = true;
        public bool useToolkitDashboard = false;
        public bool useSceneVisualization = false;
        public bool useDebugOutput = true;
        
        [Header("🔄 Tracking")]
        public TrackerType trackerType = TrackerType.IOU;
        [Range(0.1f, 0.9f)] public float trackingIoUThreshold = 0.3f;
        [Range(1, 30)] public int maxMissedFrames = 10;
        
        [Header("🛡️ Безопасность")]
        [Tooltip("Максимум ошибок inference подряд до остановки")]
        public int maxConsecutiveErrors = 10;
        [Tooltip("Логировать подробности каждого кадра")]
        public bool verboseLogging = false;
        
        public bool IsRunning { get; private set; }
        public InferenceResult LastResult { get; private set; }
        public VisionFrameResult LastVisionResult { get; private set; }
        public float CurrentFPS { get; private set; }
        public int ConsecutiveErrors { get; private set; }

        /// <summary>
        /// Production-facing result event. The legacy InferenceResult pipeline remains active for compatibility.
        /// </summary>
        public event Action<VisionFrameResult> OnVisionFrameResult;
        
        #endregion
        
        #region Public Methods
        
        public void StartTracking()
        {
            if (IsRunning)
            {
                Debug.LogWarning("⚠️ [TrackerManager] Трекинг уже запущен!");
                return;
            }

            if (modelConfigs != null)
            {
                for (int i = 0; i < modelConfigs.Length; i++)
                {
                    var cfg = modelConfigs[i];
                    if (cfg != null && cfg.backend == Unity.InferenceEngine.BackendType.GPUCompute)
                        Debug.LogWarning($"⚠️ [TrackerManager] ModelConfig[{i}] использует GPUCompute. На этой машине он крашит Unity. Рекомендую GPUPixel.");
                }
            }
            
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("🚀 [TrackerManager] НАЧАЛО ЗАПУСКА");
            Debug.Log("═══════════════════════════════════════");
            
            try
            {
                Debug.Log("📍 ШАГ 1/4: Инициализация Input Provider...");
                InitializeInput();
                
                if (inputProvider == null)
                {
                    Debug.LogError("❌ [TrackerManager] Input Provider не создан, запуск отменён");
                    return;
                }
                Debug.Log("✅ ШАГ 1/4 ЗАВЕРШЁН");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"💥 [TrackerManager] КРАШ на ШАГЕ 1 (Input): {e.Message}");
                Debug.LogError($"   StackTrace: {e.StackTrace}");
                CleanupOnError();
                return;
            }
            
            try
            {
                Debug.Log("📍 ШАГ 2/4: Инициализация моделей...");
                InitializeModels();
                
                if (activeModel == null || !activeModel.IsInitialized)
                {
                    Debug.LogError("❌ [TrackerManager] Модель не инициализирована, запуск отменён");
                    CleanupOnError();
                    return;
                }
                Debug.Log("✅ ШАГ 2/4 ЗАВЕРШЁН");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"💥 [TrackerManager] КРАШ на ШАГЕ 2 (Models): {e.Message}");
                Debug.LogError($"   StackTrace: {e.StackTrace}");
                CleanupOnError();
                return;
            }
            
            try
            {
                Debug.Log("📍 ШАГ 3/4: Инициализация Output Receivers...");
                InitializeOutputs();
                Debug.Log("✅ ШАГ 3/4 ЗАВЕРШЁН");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"💥 [TrackerManager] КРАШ на ШАГЕ 3 (Outputs): {e.Message}");
                Debug.LogError($"   StackTrace: {e.StackTrace}");
                CleanupOnError();
                return;
            }
            
            try
            {
                Debug.Log("📍 ШАГ 4/4: Инициализация Tracking...");
                InitializeTracking();
                Debug.Log("✅ ШАГ 4/4 ЗАВЕРШЁН");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"💥 [TrackerManager] КРАШ на ШАГЕ 4 (Tracking): {e.Message}");
                Debug.LogError($"   StackTrace: {e.StackTrace}");
                CleanupOnError();
                return;
            }
            
            IsRunning = true;
            lastFrameTime = Time.time;
            consecutiveErrors = 0;
            totalFramesProcessed = 0;
            
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("✅ [TrackerManager] ТРЕКИНГ УСПЕШНО ЗАПУЩЕН!");
            Debug.Log($"📊 [TrackerManager] Настройки: FPS={targetFPS}, MaxErrors={maxConsecutiveErrors}");
            Debug.Log("═══════════════════════════════════════");
        }
        
        public void StopTracking()
        {
            if (!IsRunning)
                return;
            
            IsRunning = false;
            
            try
            {
                inputProvider?.Release();
                inputProvider = null;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ [TrackerManager] Ошибка при остановке input: {e.Message}");
            }
            
            try
            {
                activeModel?.Dispose();
                activeModel = null;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ [TrackerManager] Ошибка при dispose модели: {e.Message}");
            }
            
            try
            {
                foreach (var receiver in outputReceivers)
                {
                    if (receiver != null)
                        receiver.Release();
                }
                outputReceivers.Clear();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ [TrackerManager] Ошибка при остановке receivers: {e.Message}");
            }
            
            Debug.Log("⏹️ [TrackerManager] Трекинг остановлен!");
        }
        
        public void SwitchModel(int index)
        {
            if (index < 0 || index >= modelConfigs.Length)
            {
                Debug.LogError($"❌ [TrackerManager] Неверный индекс модели: {index}");
                return;
            }
            
            activeModel?.Dispose();
            activeModelIndex = index;
            InitializeModels();
            
            Debug.Log($"🔄 [TrackerManager] Переключена модель: {modelConfigs[index].modelName}");
        }
        
        public void SetInputProvider(IInputProvider provider)
        {
            inputProvider?.Release();
            inputProvider = provider;
            inputProvider?.Initialize();
        }
        
        public TrackedObject[] GetTrackedObjects()
        {
            if (tracker == null) return new TrackedObject[0];
            return tracker.GetAllTrackedObjects();
        }
        
        #endregion
        
        #region Private Fields
        
        private IInputProvider inputProvider;
        private IInferenceModel activeModel;
        private List<IOutputReceiver> outputReceivers = new List<IOutputReceiver>();
        private ITracker tracker;
        
        private float lastFrameTime;
        private float frameInterval;
        private int frameCount;
        private float fpsTimer;
        private int consecutiveErrors;
        private int totalFramesProcessed;
        
        #endregion
        
        #region Unity Lifecycle
        
        void Start()
        {
            frameInterval = 1f / targetFPS;
            
            if (autoStart)
            {
                // Запуск с задержкой - даём время камере инициализироваться
                Invoke(nameof(DelayedStart), 0.5f);
            }
        }
        
        void DelayedStart()
        {
            StartTracking();
        }
        
        void Update()
        {
            if (!IsRunning) return;
            
            bool isFirstUpdates = totalFramesProcessed < 15;
            
            if (isFirstUpdates && Time.frameCount % 5 == 0)
                Debug.Log($"📡 [TrackerManager] Update() - Unity Frame {Time.frameCount}, IsRunning: {IsRunning}");
            
            // Логируем состояние input provider раз в секунду если не готов
            if (inputProvider == null)
            {
                if (Time.frameCount % 60 == 0)
                    Debug.LogWarning("⚠️ [TrackerManager] Input Provider is NULL!");
                return;
            }
            
            // ВАЖНО: Обновляем текстуру ВСЕГДА (для увеличения счётчика кадров)
            if (isFirstUpdates)
                Debug.Log($"   → Вызов inputProvider.UpdateTexture()...");
            
            inputProvider.UpdateTexture();
            
            if (isFirstUpdates)
                Debug.Log($"   ← UpdateTexture() завершился");
            
            if (!inputProvider.IsReady)
            {
                if (Time.frameCount % 60 == 0 || isFirstUpdates)
                    Debug.LogWarning($"⚠️ [TrackerManager] Input Provider ({inputProvider.GetType().Name}) НЕ готов! (ждём инициализации...)");
                return;
            }
            
            if (isFirstUpdates)
                Debug.Log($"   ✓ Input Provider готов!");
            
            // Пропускаем первые кадры пока камера не стабилизируется
            if (totalFramesProcessed < 10)
            {
                var tex = inputProvider.CurrentTexture;
                if (tex != null)
                {
                    Debug.Log($"🎥 [TrackerManager] Стабилизация {totalFramesProcessed}/10 (tex: {tex.width}x{tex.height}, type: {tex.GetType().Name})");
                    totalFramesProcessed++;
                }
                else
                {
                    if (Time.frameCount % 60 == 0)
                        Debug.LogWarning($"⚠️ [TrackerManager] CurrentTexture is NULL на кадре {totalFramesProcessed}!");
                }
                return;
            }
            
            if (Time.time - lastFrameTime < frameInterval)
                return;
            
            lastFrameTime = Time.time;
            
            UpdateFPS();
            ProcessFrame();
        }
        
        void OnDestroy()
        {
            StopTracking();
        }
        
        #endregion
        
        #region Private Methods - Initialization
        
        private void InitializeInput()
        {
            try
            {
                Debug.Log("🔍 [TrackerManager] Начало InitializeInput...");
                Debug.Log($"   customInputProvider: {(customInputProvider != null ? customInputProvider.GetType().Name : "null")}");
                Debug.Log($"   inputType: {inputType}");
                
                if (customInputProvider != null && customInputProvider is IInputProvider provider)
                {
                    Debug.Log($"📌 [TrackerManager] Используем привязанный Input Provider: {customInputProvider.GetType().Name}");
                    inputProvider = provider;
                    
                    // Если провайдер уже на сцене - не инициализируем повторно
                    Debug.Log($"🎬 [TrackerManager] Инициализация {inputProvider.GetType().Name}...");
                    inputProvider.Initialize();
                    Debug.Log($"✅ [TrackerManager] Input Provider инициализирован");
                }
                else if (customInputProvider != null)
                {
                    Debug.LogError($"❌ [TrackerManager] customInputProvider ({customInputProvider.GetType().Name}) НЕ реализует IInputProvider!");
                    return;
                }
                else
                {
                    Debug.Log($"🔧 [TrackerManager] Создаём Input Provider динамически: {inputType}");
                    inputProvider = inputType switch
                    {
                        InputProviderType.WebCam => gameObject.AddComponent<WebCamInputProvider>(),
                        InputProviderType.Camera => gameObject.AddComponent<CameraInputProvider>(),
                        InputProviderType.Texture => gameObject.AddComponent<TextureInputProvider>(),
                        InputProviderType.Video => gameObject.AddComponent<VideoInputProvider>(),
                        _ => null
                    };
                    
                    if (inputProvider == null)
                    {
                        Debug.LogError("❌ [TrackerManager] Не удалось создать Input Provider!");
                        return;
                    }
                    
                    Debug.Log($"🎬 [TrackerManager] Инициализация динамического {inputProvider.GetType().Name}...");
                    inputProvider.Initialize();
                }
                
                Debug.Log($"✅ [TrackerManager] Input Provider готов. IsReady: {inputProvider.IsReady}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ [TrackerManager] КРИТИЧЕСКАЯ ОШИБКА инициализации input: {e.Message}");
                Debug.LogError($"   StackTrace: {e.StackTrace}");
                inputProvider = null;
            }
        }
        
        private void InitializeModels()
        {
            try
            {
                Debug.Log("🔍 [TrackerManager] Начало InitializeModels...");
                
                if (modelConfigs == null || modelConfigs.Length == 0)
                {
                    Debug.LogError("❌ [TrackerManager] Конфигурации моделей не заданы!");
                    return;
                }
                
                Debug.Log($"   Найдено конфигов: {modelConfigs.Length}, activeIndex: {activeModelIndex}");
                
                if (activeModelIndex >= modelConfigs.Length)
                    activeModelIndex = 0;
                
                var config = modelConfigs[activeModelIndex];
                
                if (config == null)
                {
                    Debug.LogError($"❌ [TrackerManager] ModelConfig[{activeModelIndex}] is null!");
                    return;
                }
                
                Debug.Log($"   Config: {config.modelName}, Asset: {(config.modelAsset != null ? "OK" : "NULL")}");
                
                if (config.modelAsset == null)
                {
                    Debug.LogError($"❌ [TrackerManager] ModelAsset null в конфиге '{config.modelName}'!");
                    return;
                }
                
                Debug.Log($"🏭 [TrackerManager] Создаём модель через фабрику...");
                activeModel = YOLOModelFactory.CreateModel(config);
                
                if (activeModel == null)
                {
                    Debug.LogError("❌ [TrackerManager] Фабрика вернула null!");
                    return;
                }
                
                Debug.Log($"🧠 [TrackerManager] Инициализация модели {config.modelName}...");
                activeModel.Initialize(config);
                
                if (!activeModel.IsInitialized)
                {
                    Debug.LogError("❌ [TrackerManager] Модель не инициализировалась!");
                    activeModel.Dispose();
                    activeModel = null;
                }
                else
                {
                    Debug.Log($"✅ [TrackerManager] Модель {config.modelName} готова!");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ [TrackerManager] КРИТИЧЕСКАЯ ОШИБКА инициализации модели: {e.Message}");
                Debug.LogError($"   StackTrace: {e.StackTrace}");
                if (activeModel != null)
                {
                    try { activeModel.Dispose(); } catch { }
                    activeModel = null;
                }
            }
        }
        
        private void InitializeOutputs()
        {
            outputReceivers.Clear();
            
            // 1. Event Output
            try
            {
                if (manualEventReceiver != null)
                {
                    Debug.Log("📌 [TrackerManager] Используем привязанный Event Receiver");
                    manualEventReceiver.Initialize();
                    outputReceivers.Add(manualEventReceiver);
                }
                else if (useEventOutput)
                {
                    Debug.Log("🔧 [TrackerManager] Создаём Event Receiver динамически");
                    var eventOutput = gameObject.AddComponent<EventOutputReceiver>();
                    eventOutput.Initialize();
                    outputReceivers.Add(eventOutput);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ [TrackerManager] Ошибка EventOutput: {e.Message}");
            }
            
            // 2. UI Visualization
            try
            {
                if (manualUIReceiver != null)
                {
                    Debug.Log("📌 [TrackerManager] Используем привязанный UI Receiver");
                    manualUIReceiver.Initialize();
                    outputReceivers.Add(manualUIReceiver);
                }
                else if (useUIVisualization)
                {
                    Debug.Log("🔧 [TrackerManager] Создаём UI Receiver динамически");
                    var uiOutput = gameObject.AddComponent<UIVisualizationReceiver>();
                    uiOutput.Initialize();
                    outputReceivers.Add(uiOutput);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ [TrackerManager] Ошибка UIOutput: {e.Message}");
            }
            
            // 2b. UI Toolkit Dashboard
            try
            {
                if (manualToolkitDashboardReceiver != null)
                {
                    Debug.Log("[TrackerManager] Using bound UI Toolkit Dashboard");
                    manualToolkitDashboardReceiver.trackerManager = this;
                    manualToolkitDashboardReceiver.Initialize();
                    outputReceivers.Add(manualToolkitDashboardReceiver);
                }
                else if (useToolkitDashboard)
                {
                    Debug.Log("[TrackerManager] Creating UI Toolkit Dashboard dynamically");
                    var dashboardOutput = gameObject.AddComponent<VisionToolkitDashboardReceiver>();
                    dashboardOutput.trackerManager = this;
                    dashboardOutput.Initialize();
                    outputReceivers.Add(dashboardOutput);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[TrackerManager] ToolkitDashboard error: {e.Message}");
            }
            
            // 3. Scene Visualization
            try
            {
                if (manualSceneReceiver != null)
                {
                    Debug.Log("📌 [TrackerManager] Используем привязанный Scene Receiver");
                    manualSceneReceiver.Initialize();
                    outputReceivers.Add(manualSceneReceiver);
                }
                else if (useSceneVisualization)
                {
                    Debug.Log("🔧 [TrackerManager] Создаём Scene Receiver динамически");
                    var sceneOutput = gameObject.AddComponent<SceneVisualizationReceiver>();
                    sceneOutput.Initialize();
                    outputReceivers.Add(sceneOutput);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ [TrackerManager] Ошибка SceneOutput: {e.Message}");
            }
            
            // 4. Debug Output
            try
            {
                if (manualDebugReceiver != null)
                {
                    Debug.Log("📌 [TrackerManager] Используем привязанный Debug Receiver");
                    manualDebugReceiver.Initialize();
                    outputReceivers.Add(manualDebugReceiver);
                }
                else if (useDebugOutput)
                {
                    Debug.Log("🔧 [TrackerManager] Создаём Debug Receiver динамически");
                    var debugOutput = gameObject.AddComponent<DebugOutputReceiver>();
                    debugOutput.Initialize();
                    outputReceivers.Add(debugOutput);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ [TrackerManager] Ошибка DebugOutput: {e.Message}");
            }
            
            Debug.Log($"✅ [TrackerManager] Инициализировано {outputReceivers.Count} output receivers");
        }
        
        private void InitializeTracking()
        {
            if (!useTracking) return;
            
            tracker = trackerType switch
            {
                TrackerType.IOU => new IOUTracker(trackingIoUThreshold, maxMissedFrames),
                TrackerType.SORT => new SORTTracker(trackingIoUThreshold, maxMissedFrames),
                _ => null
            };
        }
        
        #endregion
        
        #region Private Methods - Processing
        
        private void ProcessFrame()
        {
            try
            {
                totalFramesProcessed++;
                
                bool isFirstFrames = totalFramesProcessed <= 10;
                
                if (isFirstFrames)
                    Debug.Log($"🎬 [TrackerManager] ═══ КАДР #{totalFramesProcessed} ═══");
                
                if (verboseLogging && totalFramesProcessed % 30 == 0)
                    Debug.Log($"📊 [TrackerManager] Обработано кадров: {totalFramesProcessed}, Ошибок подряд: {consecutiveErrors}");
                
                if (inputProvider == null || !inputProvider.IsReady)
                {
                    if (isFirstFrames || verboseLogging)
                        Debug.LogWarning($"⚠️ [TrackerManager] Input provider не готов (IsReady: {inputProvider?.IsReady})");
                    return;
                }
                
                if (isFirstFrames)
                    Debug.Log($"   ✓ Input Provider готов");
                
                // UpdateTexture уже вызван в Update()
                
                var inputTexture = inputProvider.CurrentTexture;
                if (inputTexture == null)
                {
                    if (isFirstFrames || verboseLogging)
                        Debug.LogWarning("⚠️ [TrackerManager] Input texture null");
                    return;
                }
                
                if (isFirstFrames)
                    Debug.Log($"   ✓ Текстура получена: {inputTexture.width}x{inputTexture.height}");
                
                if (activeModel == null || !activeModel.IsInitialized)
                {
                    Debug.LogError("❌ [TrackerManager] Модель не инициализирована!");
                    consecutiveErrors++;
                    CheckErrorThreshold();
                    return;
                }
                
                if (isFirstFrames)
                    Debug.Log($"   ✓ Модель готова");
                
                InferenceResult result = null;
                
                try
                {
                    if (isFirstFrames)
                        Debug.Log($"   🧠 Запуск inference...");
                    
                    result = activeModel.RunInference(inputTexture);
                    
                    if (isFirstFrames)
                        Debug.Log($"   ✅ Inference завершён!");
                    
                    if (result != null && result.success)
                    {
                        consecutiveErrors = 0; // Сбрасываем счетчик при успехе
                        
                        if (verboseLogging)
                            Debug.Log($"✅ [TrackerManager] Inference: {result.inferenceTime:F1}ms, Детекций: {result.detectionCount}");
                    }
                    else
                    {
                        consecutiveErrors++;
                        Debug.LogWarning($"⚠️ [TrackerManager] Inference не удался (ошибка #{consecutiveErrors})");
                        CheckErrorThreshold();
                        return;
                    }
                }
                catch (System.Exception e)
                {
                    consecutiveErrors++;
                    Debug.LogError($"❌ [TrackerManager] Ошибка inference (#{consecutiveErrors}): {e.Message}");
                    CheckErrorThreshold();
                    return;
                }
                
                if (result == null)
                {
                    consecutiveErrors++;
                    Debug.LogWarning($"⚠️ [TrackerManager] Результат inference null (#{consecutiveErrors})");
                    CheckErrorThreshold();
                    return;
                }
                
                if (result.success && useTracking && tracker != null && result.boundingBoxes != null)
                {
                    try
                    {
                        var tracked = tracker.Update(result.boundingBoxes, Time.deltaTime);
                        
                        for (int i = 0; i < result.boundingBoxes.Length && i < tracked.Length; i++)
                            result.boundingBoxes[i].id = tracked[i].id;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"❌ [TrackerManager] Ошибка tracking: {e.Message}");
                    }
                }
                
                LastResult = result;
                LastVisionResult = VisionResultAdapter.FromInferenceResult(
                    result,
                    inputTexture,
                    totalFramesProcessed,
                    Time.realtimeSinceStartupAsDouble);
                ConsecutiveErrors = consecutiveErrors;
                OnVisionFrameResult?.Invoke(LastVisionResult);
                
                foreach (var receiver in outputReceivers)
                {
                    try
                    {
                        if (receiver != null && receiver.IsEnabled)
                            receiver.ReceiveResult(result, inputTexture);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"❌ [TrackerManager] Ошибка в receiver: {e.Message}");
                    }
                }
            }
            catch (System.Exception e)
            {
                consecutiveErrors++;
                Debug.LogError($"❌ [TrackerManager] КРИТИЧЕСКАЯ ОШИБКА ProcessFrame (#{consecutiveErrors}): {e.Message}\n{e.StackTrace}");
                CheckErrorThreshold();
            }
        }
        
        private void CheckErrorThreshold()
        {
            if (consecutiveErrors >= maxConsecutiveErrors)
            {
                Debug.LogError($"🛑 [TrackerManager] ПРЕВЫШЕН ЛИМИТ ОШИБОК ({consecutiveErrors}/{maxConsecutiveErrors})! Останавливаем трекинг!");
                StopTracking();
            }
        }
        
        private void UpdateFPS()
        {
            frameCount++;
            fpsTimer += Time.deltaTime;
            
            if (fpsTimer >= 1f)
            {
                CurrentFPS = frameCount / fpsTimer;
                frameCount = 0;
                fpsTimer = 0f;
            }
        }
        
        private void CleanupOnError()
        {
            IsRunning = false;
            
            try { inputProvider?.Release(); } catch { }
            try { activeModel?.Dispose(); } catch { }
            
            foreach (var receiver in outputReceivers)
            {
                try { receiver?.Release(); } catch { }
            }
            
            outputReceivers.Clear();
            inputProvider = null;
            activeModel = null;
        }
        
        #endregion
    }
    
    #region Enums
    
    public enum InputProviderType
    {
        WebCam,
        Camera,
        Texture,
        Video,
        Custom
    }
    
    public enum TrackerType
    {
        None,
        IOU,
        SORT
    }
    
    #endregion
}
