using UnityEngine;
using UnityEngine.UIElements;
using UniversalTracker;
using UniversalTracker.Core;
using UniversalTracker.OutputReceivers;

namespace ProAnimaVision.Samples
{
    [DisallowMultipleComponent]
    public sealed class ProAnimaVisionExperimentalSceneBootstrap : MonoBehaviour
    {
        [Header("WebCam Preview")]
        public bool runWebCamPreview = true;
        public int deviceIndex;
        public string deviceNameOverride;
        [Min(16)] public int requestedWidth = 1280;
        [Min(16)] public int requestedHeight = 720;
        [Range(1, 120)] public int requestedFps = 30;
        public ScaleMode previewScaleMode = ScaleMode.ScaleToFit;
        public int previewRotationDegrees;
        public bool mirrorPreviewX;
        public bool mirrorPreviewY;

        [Header("Real Pipeline")]
        public bool configureRealPipeline;
        public VisionPipelineProfile pipelineProfile;
        public VisionModelProfile modelProfile;
        public bool autoStartRealPipeline;

        [Header("Fallback")]
        public bool syntheticFallbackWhenNoCamera = true;

        private UIDocument document;
        private PanelSettings panelSettings;
        private VisionToolkitDashboardReceiver dashboard;
        private UniversalTrackerManager manager;
        private WebCamTexture webCamTexture;
        private Texture2D fallbackTexture;
        private Color32[] fallbackPixels;
        private int frameIndex;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            EnsureCamera();
            EnsureDashboard();
            EnsureManager();
            StartWebCamPreview();
        }

        private void Start()
        {
            if (configureRealPipeline && autoStartRealPipeline && manager != null)
                manager.StartTracking();
        }

        private void Update()
        {
            ApplyDashboardPreviewSettings();

            if (configureRealPipeline)
                return;

            if (runWebCamPreview && webCamTexture != null && webCamTexture.isPlaying)
            {
                dashboard.ReceiveVisionResult(CreatePreviewResult(webCamTexture), webCamTexture);
                return;
            }

            if (syntheticFallbackWhenNoCamera)
            {
                UpdateFallbackTexture();
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
            RestartWebCamPreview();
        }

        private void StartWebCamPreview()
        {
            if (!runWebCamPreview)
                return;

            string deviceName = ResolveDeviceName();
            webCamTexture = string.IsNullOrWhiteSpace(deviceName)
                ? new WebCamTexture(requestedWidth, requestedHeight, requestedFps)
                : new WebCamTexture(deviceName, requestedWidth, requestedHeight, requestedFps);
            webCamTexture.Play();
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
            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.name = "ProAnima Vision WebCam Panel";
            document.panelSettings = panelSettings;

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
            manager.pipelineProfile = pipelineProfile;
            manager.modelProfiles = pipelineProfile == null && modelProfile != null ? new[] { modelProfile } : null;
            manager.inputType = InputProviderType.WebCam;
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
            dashboard.trackerManager = manager;
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
