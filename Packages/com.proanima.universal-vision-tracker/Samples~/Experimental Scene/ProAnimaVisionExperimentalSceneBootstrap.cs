using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UIElements;
using UniversalTracker;
using UniversalTracker.Core;
using UniversalTracker.OutputReceivers;

namespace ProAnimaVision.Samples
{
    [DisallowMultipleComponent]
    public sealed partial class ProAnimaVisionExperimentalSceneBootstrap : MonoBehaviour
    {
        [Header("WebCam Preview")]
        [Tooltip("Run a direct webcam preview when the real model pipeline is disabled.")]
        public bool runWebCamPreview = true;
        [Tooltip("Index of the webcam device used when no explicit device name is selected.")]
        public int deviceIndex;
        [Tooltip("Exact webcam device name selected from runtime controls or entered manually.")]
        public string deviceNameOverride;
        [Tooltip("Requested webcam width in pixels.")]
        [Min(16)] public int requestedWidth = 1280;
        [Tooltip("Requested webcam height in pixels.")]
        [Min(16)] public int requestedHeight = 720;
        [Tooltip("Requested webcam frame rate.")]
        [Range(1, 120)] public int requestedFps = 30;
        [Tooltip("How the webcam preview fits inside the dashboard viewport.")]
        public ScaleMode previewScaleMode = ScaleMode.ScaleToFit;
        [Tooltip("Clockwise preview rotation in degrees.")]
        public int previewRotationDegrees;
        [Tooltip("Mirror the preview horizontally.")]
        public bool mirrorPreviewX;
        [Tooltip("Mirror the preview vertically.")]
        public bool mirrorPreviewY;

        [Header("Real Pipeline")]
        [Tooltip("Use UniversalTrackerManager with the selected profiles instead of preview-only webcam mode.")]
        public bool configureRealPipeline;
        [Tooltip("Pipeline profile to run in the demo scene.")]
        public VisionPipelineProfile pipelineProfile;
        [Tooltip("Compatibility model profile used when no pipeline profile is assigned.")]
        public VisionModelProfile modelProfile;
        [Tooltip("Start the real pipeline automatically on scene start.")]
        public bool autoStartRealPipeline;
        [Tooltip("Frame source used by UniversalTrackerManager when real pipeline mode is enabled.")]
        public InputProviderType realPipelineSource = InputProviderType.WebCam;
        [Tooltip("VideoPlayer used when Real Pipeline Source is Video. Assign a Video Clip or URL here.")]
        public VideoPlayer sourceVideoPlayer;
        [Tooltip("Optional ordered video row for Previous/Next runtime controls.")]
        public VisionVideoPlaylistSource videoPlaylist;

        [Header("Fallback")]
        [Tooltip("Show a synthetic texture if no camera frames are available.")]
        public bool syntheticFallbackWhenNoCamera = true;

        private UIDocument document;
        private PanelSettings panelSettings;
        private VisionToolkitDashboardReceiver dashboard;
        private UniversalTrackerManager manager;
        private WebCamTexture webCamTexture;
        private Texture2D fallbackTexture;
        private Color32[] fallbackPixels;
        private int frameIndex;
        private VisionHealthStatus previewHealth;
        private DropdownField cameraDropdown;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            EnsureCamera();
            EnsureDashboard();
            EnsureManager();
            EnsureCameraControls();
            EnsureVideoControls();
            if (ShouldRunStandaloneWebCamPreview())
                StartWebCamPreview();
        }

        private void Start()
        {
            if (configureRealPipeline && autoStartRealPipeline && manager != null)
                manager.StartTracking();
        }

        private void Update()
        {
            EnsureDocumentPanelSettings();
            ApplyDashboardPreviewSettings();
            UpdateCameraControls();
            UpdateVideoControls();
            if (!ShouldRunStandaloneWebCamPreview())
                StopWebCamPreview();

            if (configureRealPipeline)
                return;

            if (ShouldRunStandaloneWebCamPreview() && webCamTexture != null && webCamTexture.isPlaying)
            {
                dashboard.SetHealthStatus(ResolvePreviewHealth(webCamTexture));
                dashboard.ReceiveVisionResult(CreatePreviewResult(webCamTexture), webCamTexture);
                return;
            }

            if (syntheticFallbackWhenNoCamera)
            {
                UpdateFallbackTexture();
                dashboard.SetHealthStatus(ResolveFallbackHealth());
                dashboard.ReceiveVisionResult(CreatePreviewResult(fallbackTexture), fallbackTexture);
            }
        }

        private void OnDestroy()
        {
            if (webCamTexture != null)
                webCamTexture.Stop();

            if (fallbackTexture != null)
                Destroy(fallbackTexture);

            if (panelSettings != null)
                Destroy(panelSettings);
        }

        [ContextMenu("Restart WebCam Preview")]
        public void RestartWebCamPreview()
        {
            StopWebCamPreview();
            StartWebCamPreview();
        }

        [ContextMenu("Use Next WebCam")]
        public void UseNextWebCam()
        {
            WebCamDevice[] devices = WebCamTexture.devices;
            if (devices == null || devices.Length == 0)
                return;

            deviceIndex = (deviceIndex + 1) % devices.Length;
            deviceNameOverride = null;
            RefreshCameraDropdown();
            RestartWebCamPreview();
        }

        private void StartWebCamPreview()
        {
            if (!ShouldRunStandaloneWebCamPreview())
                return;

            string deviceName = ResolveDeviceName();
            webCamTexture = string.IsNullOrWhiteSpace(deviceName)
                ? new WebCamTexture(requestedWidth, requestedHeight, requestedFps)
                : new WebCamTexture(deviceName, requestedWidth, requestedHeight, requestedFps);
            webCamTexture.Play();
        }

        private bool ShouldRunStandaloneWebCamPreview()
        {
            return runWebCamPreview && !configureRealPipeline && realPipelineSource == InputProviderType.WebCam;
        }

        private void StopWebCamPreview()
        {
            if (webCamTexture == null)
                return;

            webCamTexture.Stop();
            webCamTexture = null;
        }

        private string ResolveDeviceName()
        {
            if (!string.IsNullOrWhiteSpace(deviceNameOverride))
                return deviceNameOverride;

            WebCamDevice[] devices = WebCamTexture.devices;
            if (devices == null || devices.Length == 0)
                return null;

            int index = Mathf.Clamp(deviceIndex, 0, devices.Length - 1);
            return devices[index].name;
        }

        private void EnsureCamera()
        {
            if (Camera.main != null)
                return;

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.02f, 0.024f, 1f);
            cameraObject.AddComponent<AudioListener>();
        }

        private void EnsureDashboard()
        {
            document = GetComponent<UIDocument>() ?? gameObject.AddComponent<UIDocument>();
            EnsureDocumentPanelSettings();

            dashboard = GetComponent<VisionToolkitDashboardReceiver>() ?? gameObject.AddComponent<VisionToolkitDashboardReceiver>();
            dashboard.autoFindManager = false;
            dashboard.subscribeToManagerEvent = configureRealPipeline;
            dashboard.showStats = true;
            dashboard.showOverlayMetrics = true;
            dashboard.showDetections = true;
            dashboard.showPoses = true;
            dashboard.showMasks = true;
            dashboard.maxRows = 10;
            ApplyDashboardPreviewSettings();
        }

        private void EnsureDocumentPanelSettings()
        {
            if (document == null)
                return;

            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                panelSettings.name = "ProAnima Vision WebCam Panel";
            }

            if (document.panelSettings == null)
                document.panelSettings = panelSettings;
        }

        private void ApplyDashboardPreviewSettings()
        {
            if (dashboard == null)
                return;

            dashboard.previewScaleMode = previewScaleMode;
            dashboard.previewRotationDegrees = previewRotationDegrees;
            dashboard.mirrorPreviewX = mirrorPreviewX;
            dashboard.mirrorPreviewY = mirrorPreviewY;
        }

        private void EnsureManager()
        {
            manager = GetComponent<UniversalTrackerManager>() ?? gameObject.AddComponent<UniversalTrackerManager>();
            AdoptExistingVideoSourceIfNeeded();
            manager.pipelineProfile = pipelineProfile;
            manager.modelProfiles = pipelineProfile == null && modelProfile != null ? new[] { modelProfile } : null;
            manager.inputType = realPipelineSource;
            VideoPlayer videoPlayer = realPipelineSource == InputProviderType.Video ? EnsureVideoPlayer() : null;
            if (videoPlayer != null)
                EnsureVideoPlaylist(videoPlayer).ApplyCurrent(false);
            manager.sourceVideoPlayer = videoPlayer;
            manager.webCamDeviceName = ResolveDeviceName();
            manager.webCamRequestedWidth = requestedWidth;
            manager.webCamRequestedHeight = requestedHeight;
            manager.webCamRequestedFps = requestedFps;
            manager.autoStart = configureRealPipeline && autoStartRealPipeline;
            manager.useTracking = true;
            manager.useEventOutput = false;
            manager.useDebugOutput = false;
            manager.useUIVisualization = false;
            manager.useSceneVisualization = false;
            manager.useToolkitDashboard = false;
            manager.manualToolkitDashboardReceiver = dashboard;

            if (configureRealPipeline)
            {
                dashboard.trackerManager = manager;
                dashboard.subscribeToManagerEvent = true;
                dashboard.SetCommandHandlers(manager.StartTracking, manager.StopTracking);
                return;
            }

            dashboard.trackerManager = null;
            dashboard.subscribeToManagerEvent = false;
            dashboard.SetCommandHandlers(RestartWebCamPreview, StopWebCamPreview);
        }

        private VideoPlayer EnsureVideoPlayer()
        {
            if (sourceVideoPlayer == null)
                sourceVideoPlayer = GetComponent<VideoPlayer>() ?? gameObject.AddComponent<VideoPlayer>();

            VisionVideoPlaylistSource.ConfigureVideoPlayerDefaults(sourceVideoPlayer);
            return sourceVideoPlayer;
        }

        private void AdoptExistingVideoSourceIfNeeded()
        {
            if (manager == null ||
                realPipelineSource != InputProviderType.WebCam ||
                manager.inputType != InputProviderType.Video)
            {
                return;
            }

            realPipelineSource = InputProviderType.Video;
            runWebCamPreview = false;
            sourceVideoPlayer = sourceVideoPlayer != null
                ? sourceVideoPlayer
                : manager.sourceVideoPlayer != null
                    ? manager.sourceVideoPlayer
                    : GetComponent<VideoPlayer>();
            videoPlaylist = videoPlaylist != null
                ? videoPlaylist
                : GetComponent<VisionVideoPlaylistSource>();
        }

        private VisionFrameResult CreatePreviewResult(Texture texture)
        {
            Vector2Int size = texture != null
                ? new Vector2Int(Mathf.Max(1, texture.width), Mathf.Max(1, texture.height))
                : new Vector2Int(requestedWidth, requestedHeight);

            return new VisionFrameResult
            {
                frameIndex = frameIndex++,
                timestamp = Time.timeAsDouble,
                sourceSize = size,
                stats = VisionPerformanceStats.FromStages(0f, 0f, 0f, 0f)
            };
        }

        private VisionHealthStatus ResolvePreviewHealth(Texture texture)
        {
            if (texture == null)
            {
                previewHealth = VisionHealthStatus.Create(
                    VisionHealthState.Degraded,
                    previewHealth?.state ?? VisionHealthState.NotInitialized,
                    VisionHealthEvent.Degraded,
                    "No camera texture is available.",
                    new VisionError(VisionErrorCode.SourceNotReady, "No WebCamTexture is available."));
                return previewHealth;
            }

            if (texture.width <= 16 || texture.height <= 16)
            {
                previewHealth = VisionHealthStatus.Create(
                    VisionHealthState.Degraded,
                    previewHealth?.state ?? VisionHealthState.NotInitialized,
                    VisionHealthEvent.Degraded,
                    "Camera is warming up.",
                    new VisionError(VisionErrorCode.SourceNotReady, "Waiting for the selected camera to deliver frames."));
                return previewHealth;
            }

            previewHealth = VisionHealthStatus.Create(
                VisionHealthState.Running,
                previewHealth?.state ?? VisionHealthState.NotInitialized,
                VisionHealthEvent.Recovered,
                "WebCam preview is running.");
            return previewHealth;
        }

        private VisionHealthStatus ResolveFallbackHealth()
        {
            previewHealth = VisionHealthStatus.Create(
                VisionHealthState.Degraded,
                previewHealth?.state ?? VisionHealthState.NotInitialized,
                VisionHealthEvent.Degraded,
                "No camera frame is available.",
                new VisionError(VisionErrorCode.SourceNotReady, "Showing synthetic fallback until a camera delivers frames."));
            return previewHealth;
        }

        private void UpdateFallbackTexture()
        {
            if (fallbackTexture == null)
            {
                fallbackTexture = new Texture2D(320, 180, TextureFormat.RGBA32, false);
                fallbackTexture.name = "ProAnima Vision Camera Fallback";
                fallbackPixels = new Color32[fallbackTexture.width * fallbackTexture.height];
            }

            float pulse = Mathf.Sin(Time.time * 1.6f) * 0.5f + 0.5f;
            Color left = Color.Lerp(new Color(0.035f, 0.06f, 0.075f), new Color(0.05f, 0.1f, 0.11f), pulse);
            Color right = Color.Lerp(new Color(0.12f, 0.13f, 0.17f), new Color(0.18f, 0.12f, 0.16f), pulse);

            for (int y = 0; y < fallbackTexture.height; y++)
            {
                for (int x = 0; x < fallbackTexture.width; x++)
                {
                    float t = (float)x / Mathf.Max(1, fallbackTexture.width - 1);
                    fallbackPixels[y * fallbackTexture.width + x] = Color.Lerp(left, right, t);
                }
            }

            fallbackTexture.SetPixels32(fallbackPixels);
            fallbackTexture.Apply(false);
        }
    }
}
