using System;
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

        [Header("WebCam")]
        public string webCamDeviceName;
        [Min(16)] public int webCamRequestedWidth = 1280;
        [Min(16)] public int webCamRequestedHeight = 720;
        [Range(1, 120)] public int webCamRequestedFps = 30;

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
        public VisionHealthStatus HealthStatus => healthHub.Status;
        public VisionHealthState HealthState => healthHub.State;
        public VisionError LastError => healthHub.LastError;
        public float CurrentFPS { get; private set; }
        public int ConsecutiveErrors { get; private set; }

        public event Action<VisionFrameResult> OnVisionFrameResult;
        public event Action<VisionHealthStatus> OnVisionHealthChanged { add => healthHub.Changed += value; remove => healthHub.Changed -= value; }
        public event Action<VisionHealthStatus> OnVisionStarted { add => healthHub.Started += value; remove => healthHub.Started -= value; }
        public event Action<VisionHealthStatus> OnVisionStopped { add => healthHub.Stopped += value; remove => healthHub.Stopped -= value; }
        public event Action<VisionHealthStatus> OnVisionDegraded { add => healthHub.Degraded += value; remove => healthHub.Degraded -= value; }
        public event Action<VisionHealthStatus> OnVisionFailed { add => healthHub.Failed += value; remove => healthHub.Failed -= value; }
        public event Action<VisionHealthStatus> OnVisionRecovered { add => healthHub.Recovered += value; remove => healthHub.Recovered -= value; }

        private readonly VisionHealthEventHub healthHub = new VisionHealthEventHub();
        private readonly VisionOutputReceiverHub outputHub = new VisionOutputReceiverHub();
        private readonly VisionTrackingStage trackingStage = new VisionTrackingStage();
        private VisionPipeline visionPipeline;
        private IVisionFrameSource frameSource;
        private VisionAdapterRegistry adapterRegistry;
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
                outputHub.Initialize(gameObject, this, CreateOutputSettings());
                trackingStage.Configure(useTracking, trackerType, trackingIoUThreshold, maxMissedFrames);
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
            outputHub.Release();
            trackingStage.Reset();
            if (emitStopped)
                EmitHealth(VisionHealthStatus.Create(VisionHealthState.Stopped, HealthState, VisionHealthEvent.Stopped, "Tracker stopped."));
        }

        public void SwitchModel(int index)
        {
            VisionModelProfile[] profiles = VisionModelProfileResolver.GetProfiles(pipelineProfile, modelProfiles);
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

        public void SetTargetFps(int fps)
        {
            targetFPS = Mathf.Clamp(fps, 1, 120);
            frameInterval = 1f / targetFPS;
            if (pipelineProfile != null)
                pipelineProfile.targetFps = targetFPS;
        }

        public void SetModelThresholds(float confidenceThreshold, float nmsThreshold)
        {
            VisionModelProfile profile = ActiveModelProfile ?? VisionModelProfileResolver.Resolve(pipelineProfile, modelProfiles, ref activeModelIndex);
            if (profile == null)
                return;

            profile.confidenceThreshold = Mathf.Clamp(confidenceThreshold, 0.01f, 0.99f);
            profile.nmsThreshold = Mathf.Clamp(nmsThreshold, 0.01f, 0.99f);
        }

        public TrackedObject[] GetTrackedObjects()
        {
            return trackingStage.TrackedObjects;
        }

        private bool InitializePipeline()
        {
            ActiveModelProfile = VisionModelProfileResolver.Resolve(pipelineProfile, modelProfiles, ref activeModelIndex);
            if (ActiveModelProfile == null)
            {
                Debug.LogError("[TrackerManager] VisionModelProfile is required.");
                return false;
            }

            VisionProfileValidationReport validation = VisionProfileValidator.ValidateModelProfile(ActiveModelProfile);
            VisionProfileValidationLogger.Log("TrackerManager", ActiveModelProfile, validation);
            if (!validation.IsValid)
                return false;

            frameSource = VisionFrameSourceFactory.Create(CreateFrameSourceRequest());
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

        private VisionFrameSourceRequest CreateFrameSourceRequest()
        {
            return new VisionFrameSourceRequest(
                inputType,
                customInputProvider,
                sourceTexture,
                sourceRenderTexture,
                sourceCamera,
                cameraTargetTexture,
                sourceVideoPlayer,
                webCamDeviceName,
                webCamRequestedWidth,
                webCamRequestedHeight,
                webCamRequestedFps,
                () => GetComponent<VideoPlayer>(),
                texture => cameraTargetTexture = texture);
        }

        private VisionOutputReceiverSettings CreateOutputSettings()
        {
            return new VisionOutputReceiverSettings(
                manualEventReceiver,
                manualUIReceiver,
                manualToolkitDashboardReceiver,
                manualSceneReceiver,
                manualDebugReceiver,
                useEventOutput,
                useUIVisualization,
                useToolkitDashboard,
                useSceneVisualization,
                useDebugOutput);
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
            trackingStage.Apply(result, Time.deltaTime);
            LastVisionResult = result;
            OnVisionFrameResult?.Invoke(result);
            outputHub.Dispatch(result);
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
                RegisterPipelineError(error);
        }

        private void RegisterPipelineError(VisionError error)
        {
            consecutiveErrors++;
            ConsecutiveErrors = consecutiveErrors;
            EmitHealth(VisionHealthStatus.Create(
                VisionHealthState.Degraded,
                HealthState,
                VisionHealthEvent.Degraded,
                error.message,
                error,
                consecutiveErrors));

            if (verboseLogging)
                Debug.LogWarning($"[TrackerManager] Recoverable pipeline error: {error.code} {error.message}");
            CheckErrorThreshold();
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
            healthHub.Emit(status);
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
