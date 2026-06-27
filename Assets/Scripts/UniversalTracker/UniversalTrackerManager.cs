using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UniversalTracker.Core;
using UniversalTracker.OutputReceivers;
using UniversalTracker.Tracking;

namespace UniversalTracker
{
    public class UniversalTrackerManager : MonoBehaviour
    {
        [Header("Runtime")]
        public bool autoStart = true;
        [Range(1, 120)] public int targetFPS = 30;
        public bool useTracking = true;

        [Header("Source")]
        public InputProviderType inputType = InputProviderType.WebCam;
        public MonoBehaviour customInputProvider;
        public Texture sourceTexture;
        public RenderTexture sourceRenderTexture;
        public Camera sourceCamera;
        public RenderTexture cameraTargetTexture;
        public VideoPlayer sourceVideoPlayer;

        [Header("Models")]
        public VisionPipelineProfile pipelineProfile;
        public VisionModelProfile[] modelProfiles;
        public int activeModelIndex;

        [Header("Output Receivers")]
        public UIVisualizationReceiver manualUIReceiver;
        public VisionToolkitDashboardReceiver manualToolkitDashboardReceiver;
        public EventOutputReceiver manualEventReceiver;
        public SceneVisualizationReceiver manualSceneReceiver;
        public DebugOutputReceiver manualDebugReceiver;
        public bool useEventOutput = true;
        public bool useUIVisualization = true;
        public bool useToolkitDashboard;
        public bool useSceneVisualization;
        public bool useDebugOutput = true;

        [Header("Tracking")]
        public TrackerType trackerType = TrackerType.IOU;
        [Range(0.1f, 0.9f)] public float trackingIoUThreshold = 0.3f;
        [Range(1, 30)] public int maxMissedFrames = 10;

        [Header("Safety")]
        public int maxConsecutiveErrors = 10;
        public bool verboseLogging;

        public bool IsRunning { get; private set; }
        public VisionFrameResult LastVisionResult { get; private set; }
        public VisionModelProfile ActiveModelProfile { get; private set; }
        public VisionHealthStatus HealthStatus { get; private set; } =
            VisionHealthStatus.Create(VisionHealthState.NotInitialized, VisionHealthState.NotInitialized, VisionHealthEvent.None, "Tracker is not initialized.");
        public VisionHealthState HealthState => HealthStatus?.state ?? VisionHealthState.NotInitialized;
        public VisionError LastError => HealthStatus?.lastError;
        public float CurrentFPS { get; private set; }
        public int ConsecutiveErrors { get; private set; }

        public event Action<VisionFrameResult> OnVisionFrameResult;
        public event Action<VisionHealthStatus> OnVisionHealthChanged;
        public event Action<VisionHealthStatus> OnVisionStarted;
        public event Action<VisionHealthStatus> OnVisionStopped;
        public event Action<VisionHealthStatus> OnVisionDegraded;
        public event Action<VisionHealthStatus> OnVisionFailed;
        public event Action<VisionHealthStatus> OnVisionRecovered;

        private readonly List<IOutputReceiver> outputReceivers = new List<IOutputReceiver>();
        private VisionPipeline visionPipeline;
        private IVisionFrameSource frameSource;
        private VisionAdapterRegistry adapterRegistry;
        private ITracker tracker;
        private float frameInterval;
        private float lastFrameTime;
        private int totalFramesProcessed;
        private int consecutiveErrors;
        private int frameCount;
        private float fpsTimer;

        private void Start()
        {
            frameInterval = 1f / Mathf.Max(1, targetFPS);
            if (autoStart)
                Invoke(nameof(StartTracking), 0.5f);
        }

        private void Update()
        {
            if (!IsRunning)
                return;

            if (Time.time - lastFrameTime < frameInterval)
                return;

            lastFrameTime = Time.time;
            UpdateFPS();
            ProcessPipelineFrame();
        }

        private void OnDestroy()
        {
            StopTracking();
        }

        public void StartTracking()
        {
            if (IsRunning)
                return;

            try
            {
                EmitHealth(VisionHealthStatus.Create(VisionHealthState.Initializing, HealthState, VisionHealthEvent.None, "Tracker is initializing."));
                InitializeOutputs();
                InitializeTracking();
                if (!InitializePipeline())
                {
                    CleanupOnError();
                    return;
                }

                IsRunning = true;
                lastFrameTime = Time.time;
                totalFramesProcessed = 0;
                consecutiveErrors = 0;
                ConsecutiveErrors = 0;
            }
            catch (Exception e)
            {
                Debug.LogError($"[TrackerManager] Startup failed: {e.Message}");
                EmitHealth(VisionHealthStatus.Create(
                    VisionHealthState.Failed,
                    HealthState,
                    VisionHealthEvent.Failed,
                    "Tracker startup failed.",
                    new VisionError(VisionErrorCode.ModelInitializationFailed, e.Message, false, e)));
                CleanupOnError();
            }
        }

        public void StopTracking()
        {
            StopTrackingInternal(true);
        }

        private void StopTrackingInternal(bool emitStopped)
        {
            if (!IsRunning && visionPipeline == null)
            {
                if (emitStopped && HealthState != VisionHealthState.Stopped)
                    EmitHealth(VisionHealthStatus.Create(VisionHealthState.Stopped, HealthState, VisionHealthEvent.Stopped, "Tracker stopped."));
                return;
            }

            IsRunning = false;
            DisposePipeline();

            foreach (IOutputReceiver receiver in outputReceivers)
            {
                try { receiver?.Release(); } catch { }
            }

            outputReceivers.Clear();
            tracker = null;
            if (emitStopped)
                EmitHealth(VisionHealthStatus.Create(VisionHealthState.Stopped, HealthState, VisionHealthEvent.Stopped, "Tracker stopped."));
        }

        public void SwitchModel(int index)
        {
            VisionModelProfile[] profiles = GetActiveModelProfiles();
            if (profiles == null || index < 0 || index >= profiles.Length)
            {
                Debug.LogError($"[TrackerManager] Invalid model index: {index}");
                return;
            }

            activeModelIndex = index;
            if (IsRunning)
            {
                DisposePipeline();
                InitializePipeline();
            }
        }

        public TrackedObject[] GetTrackedObjects()
        {
            return tracker != null ? tracker.GetAllTrackedObjects() : Array.Empty<TrackedObject>();
        }

        private bool InitializePipeline()
        {
            ActiveModelProfile = ResolveActiveModelProfile();
            if (ActiveModelProfile == null)
            {
                Debug.LogError("[TrackerManager] VisionModelProfile is required.");
                return false;
            }

            VisionProfileValidationReport validation = VisionProfileValidator.ValidateModelProfile(ActiveModelProfile);
            LogProfileValidation(ActiveModelProfile, validation);
            if (!validation.IsValid)
                return false;

            frameSource = CreateFrameSource();
            if (frameSource == null)
            {
                Debug.LogError("[TrackerManager] IVisionFrameSource is required.");
                return false;
            }

            adapterRegistry ??= VisionAdapterRegistry.CreateDefault();
            if (!adapterRegistry.TryCreateRuntime(ActiveModelProfile, out IVisionRuntimeAdapter runtime, out string adapterError))
            {
                Debug.LogError($"[TrackerManager] {adapterError}");
                return false;
            }

            visionPipeline = new VisionPipeline();
            visionPipeline.FrameProcessed += HandlePipelineFrameProcessed;
            visionPipeline.ErrorReceived += HandlePipelineError;
            visionPipeline.HealthChanged += HandlePipelineHealthChanged;
            visionPipeline.Configure(ActiveModelProfile, frameSource, runtime);

            if (!visionPipeline.Start())
            {
                DisposePipeline();
                return false;
            }

            return true;
        }

        private IVisionFrameSource CreateFrameSource()
        {
            if (customInputProvider is IVisionFrameSource customSource)
                return customSource;

            return inputType switch
            {
                InputProviderType.Texture => sourceRenderTexture != null
                    ? new RenderTextureFrameSource(sourceRenderTexture)
                    : new TextureFrameSource(sourceTexture),
                InputProviderType.RenderTexture => new RenderTextureFrameSource(sourceRenderTexture),
                InputProviderType.Camera => new UnityCameraFrameSource(
                    sourceCamera != null ? sourceCamera : Camera.main,
                    ResolveCameraTargetTexture(),
                    cameraTargetTexture == null),
                InputProviderType.Video => sourceVideoPlayer != null
                    ? new VideoFrameSource(sourceVideoPlayer)
                    : CreateVideoFrameSourceFromComponent(),
                InputProviderType.WebCam => new WebCamFrameSource(),
                _ => null
            };
        }

        private RenderTexture ResolveCameraTargetTexture()
        {
            if (cameraTargetTexture != null)
                return cameraTargetTexture;

            cameraTargetTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            return cameraTargetTexture;
        }

        private VideoFrameSource CreateVideoFrameSourceFromComponent()
        {
            VideoPlayer player = GetComponent<VideoPlayer>();
            return player != null ? new VideoFrameSource(player) : null;
        }

        private VisionModelProfile ResolveActiveModelProfile()
        {
            VisionModelProfile[] profiles = GetActiveModelProfiles();
            if (profiles == null || profiles.Length == 0)
                return null;

            if (activeModelIndex < 0 || activeModelIndex >= profiles.Length)
                activeModelIndex = 0;

            return profiles[activeModelIndex];
        }

        private VisionModelProfile[] GetActiveModelProfiles()
        {
            if (pipelineProfile != null && pipelineProfile.HasModels)
                return pipelineProfile.models;

            return modelProfiles;
        }

        private void InitializeOutputs()
        {
            outputReceivers.Clear();
            AddReceiver(manualEventReceiver, useEventOutput, () => gameObject.AddComponent<EventOutputReceiver>());
            AddReceiver(manualUIReceiver, useUIVisualization, () => gameObject.AddComponent<UIVisualizationReceiver>());
            AddReceiver(manualToolkitDashboardReceiver, useToolkitDashboard, CreateToolkitDashboardReceiver);
            AddReceiver(manualSceneReceiver, useSceneVisualization, () => gameObject.AddComponent<SceneVisualizationReceiver>());
            AddReceiver(manualDebugReceiver, useDebugOutput, () => gameObject.AddComponent<DebugOutputReceiver>());
        }

        private VisionToolkitDashboardReceiver CreateToolkitDashboardReceiver()
        {
            var receiver = gameObject.AddComponent<VisionToolkitDashboardReceiver>();
            receiver.trackerManager = this;
            return receiver;
        }

        private void AddReceiver<T>(T manualReceiver, bool createIfMissing, Func<T> factory)
            where T : MonoBehaviour, IOutputReceiver
        {
            T receiver = manualReceiver;
            if (receiver == null && createIfMissing)
                receiver = factory();

            if (receiver == null)
                return;

            receiver.Initialize();
            outputReceivers.Add(receiver);
        }

        private void InitializeTracking()
        {
            if (!useTracking)
            {
                tracker = null;
                return;
            }

            tracker = trackerType switch
            {
                TrackerType.IOU => new IOUTracker(trackingIoUThreshold, maxMissedFrames),
                TrackerType.SORT => new SORTTracker(trackingIoUThreshold, maxMissedFrames),
                _ => null
            };
        }

        private void ProcessPipelineFrame()
        {
            totalFramesProcessed++;
            if (visionPipeline == null)
            {
                RegisterError("[TrackerManager] Pipeline is null.");
                return;
            }

            if (visionPipeline.TryProcessNext(out VisionFrameResult result))
            {
                consecutiveErrors = 0;
                ConsecutiveErrors = 0;
                LastVisionResult = result;
            }
        }

        private void HandlePipelineFrameProcessed(VisionFrameResult result)
        {
            ApplyTracking(result);
            LastVisionResult = result;
            OnVisionFrameResult?.Invoke(result);

            foreach (IOutputReceiver receiver in outputReceivers)
            {
                if (receiver == null || !receiver.IsEnabled)
                    continue;

                try
                {
                    receiver.ReceiveVisionResult(result);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[TrackerManager] Output receiver failed: {e.Message}");
                }
            }
        }

        private void ApplyTracking(VisionFrameResult result)
        {
            if (result == null || tracker == null || result.detections == null || result.detections.Length == 0)
                return;

            TrackedObject[] trackedObjects = tracker.Update(result.detections, Time.deltaTime);
            var trackedDetections = new VisionDetection[trackedObjects.Length];
            for (int i = 0; i < trackedObjects.Length; i++)
                trackedDetections[i] = trackedObjects[i].currentDetection;

            result.detections = trackedDetections;
            result.stats.trackingMs = 0f;
            result.stats.totalMs = result.stats.preprocessMs + result.stats.inferenceMs + result.stats.postprocessMs + result.stats.trackingMs;
        }

        private void HandlePipelineError(VisionError error)
        {
            if (error == null)
                return;

            if (!error.isRecoverable)
            {
                consecutiveErrors = maxConsecutiveErrors;
                ConsecutiveErrors = consecutiveErrors;
                Debug.LogError($"[TrackerManager] Pipeline failed: {error.code} {error.message}");
                CheckErrorThreshold();
                return;
            }

            if (error.code != VisionErrorCode.SourceNotReady)
                RegisterError($"[TrackerManager] Recoverable pipeline error: {error.code} {error.message}", false);
        }

        private void RegisterError(string message, bool emitHealth = true)
        {
            consecutiveErrors++;
            ConsecutiveErrors = consecutiveErrors;
            if (emitHealth)
            {
                EmitHealth(VisionHealthStatus.Create(
                    VisionHealthState.Degraded,
                    HealthState,
                    VisionHealthEvent.Degraded,
                    message,
                    new VisionError(VisionErrorCode.Unknown, message, true),
                    consecutiveErrors));
            }

            if (verboseLogging)
                Debug.LogWarning(message);
            CheckErrorThreshold();
        }

        private void CheckErrorThreshold()
        {
            if (consecutiveErrors < maxConsecutiveErrors)
                return;

            Debug.LogError($"[TrackerManager] Error threshold exceeded ({consecutiveErrors}/{maxConsecutiveErrors}).");
            if (HealthState != VisionHealthState.Failed)
            {
                EmitHealth(VisionHealthStatus.Create(
                    VisionHealthState.Failed,
                    HealthState,
                    VisionHealthEvent.Failed,
                    $"Error threshold exceeded ({consecutiveErrors}/{maxConsecutiveErrors}).",
                    LastError ?? new VisionError(VisionErrorCode.Unknown, "Error threshold exceeded.", false)));
            }

            StopTrackingInternal(false);
        }

        private void UpdateFPS()
        {
            frameCount++;
            fpsTimer += Time.deltaTime;
            if (fpsTimer < 1f)
                return;

            CurrentFPS = frameCount / fpsTimer;
            frameCount = 0;
            fpsTimer = 0f;
        }

        private void DisposePipeline()
        {
            if (visionPipeline != null)
            {
                visionPipeline.FrameProcessed -= HandlePipelineFrameProcessed;
                visionPipeline.ErrorReceived -= HandlePipelineError;
                visionPipeline.HealthChanged -= HandlePipelineHealthChanged;
                visionPipeline.Dispose();
                visionPipeline = null;
            }

            frameSource = null;
        }

        private void CleanupOnError()
        {
            DisposePipeline();
            IsRunning = false;
        }

        private void HandlePipelineHealthChanged(VisionHealthStatus status)
        {
            EmitHealth(status);
        }

        private void EmitHealth(VisionHealthStatus status)
        {
            if (status == null)
                return;

            HealthStatus = status;
            OnVisionHealthChanged?.Invoke(status);

            switch (status.eventType)
            {
                case VisionHealthEvent.Started:
                    OnVisionStarted?.Invoke(status);
                    break;
                case VisionHealthEvent.Stopped:
                    OnVisionStopped?.Invoke(status);
                    break;
                case VisionHealthEvent.Degraded:
                    OnVisionDegraded?.Invoke(status);
                    break;
                case VisionHealthEvent.Failed:
                    OnVisionFailed?.Invoke(status);
                    break;
                case VisionHealthEvent.Recovered:
                    OnVisionRecovered?.Invoke(status);
                    break;
            }
        }

        private void LogProfileValidation(VisionModelProfile profile, VisionProfileValidationReport report)
        {
            if (report == null || report.Messages.Count == 0)
                return;

            string profileName = profile != null ? profile.name : "null";
            foreach (VisionValidationMessage message in report.Messages)
            {
                string log = $"[TrackerManager] Profile validation '{profileName}': {message.code} - {message.message}";
                if (message.severity == VisionValidationSeverity.Error)
                    Debug.LogError(log);
                else if (message.severity == VisionValidationSeverity.Warning)
                    Debug.LogWarning(log);
                else
                    Debug.Log(log);
            }
        }
    }

    public enum InputProviderType
    {
        WebCam,
        Camera,
        Texture,
        RenderTexture,
        Video,
        Custom
    }

    public enum TrackerType
    {
        None,
        IOU,
        SORT
    }

}
