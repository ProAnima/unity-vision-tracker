using System;
using System.Collections.Generic;
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
        private VisionHealthStatus previewHealth;
        private DropdownField cameraDropdown;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            EnsureCamera();
            EnsureDashboard();
            EnsureManager();
            EnsureCameraControls();
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

        private void EnsureCameraControls()
        {
            VisualElement controlPanel = document.rootVisualElement.Q<VisualElement>("VisionControlPanel");
            if (controlPanel == null || controlPanel.Q<VisualElement>("WebCamControls") != null)
                return;

            var section = new VisualElement { name = "WebCamControls" };
            section.style.marginTop = 2;
            section.style.marginBottom = 14;
            section.style.paddingTop = 12;
            section.style.borderTopWidth = 1;
            section.style.borderTopColor = new Color(0.28f, 0.36f, 0.42f, 0.75f);

            var title = new Label("Camera");
            title.style.fontSize = 12;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new Color(0.78f, 0.86f, 0.9f, 1f);
            title.style.marginBottom = 6;
            section.Add(title);

            cameraDropdown = new DropdownField();
            cameraDropdown.style.marginBottom = 8;
            cameraDropdown.RegisterValueChangedCallback(evt => SelectCamera(evt.newValue));
            section.Add(cameraDropdown);
            RefreshCameraDropdown();

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;
            section.Add(row);

            row.Add(CreateSmallButton("Next", UseNextWebCam));
            row.Add(CreateSmallButton("Rotate", RotatePreview));
            row.Add(CreateSmallButton(mirrorPreviewX ? "Mirror On" : "Mirror", ToggleMirror));

            controlPanel.Insert(Mathf.Min(3, controlPanel.childCount), section);
        }

        private Button CreateSmallButton(string text, Action action)
        {
            var button = new Button(() => action?.Invoke()) { text = text };
            button.style.height = 28;
            button.style.minWidth = 78;
            button.style.flexGrow = 1f;
            button.style.marginRight = 6;
            button.style.marginBottom = 6;
            button.style.borderTopLeftRadius = 5;
            button.style.borderTopRightRadius = 5;
            button.style.borderBottomLeftRadius = 5;
            button.style.borderBottomRightRadius = 5;
            button.style.backgroundColor = new Color(0.12f, 0.16f, 0.18f, 1f);
            button.style.color = new Color(0.86f, 0.92f, 0.95f, 1f);
            return button;
        }

        private void RefreshCameraDropdown()
        {
            if (cameraDropdown == null)
                return;

            List<string> choices = GetCameraChoices();
            cameraDropdown.choices = choices;
            cameraDropdown.SetValueWithoutNotify(ResolveCameraDropdownValue(choices));
        }

        private List<string> GetCameraChoices()
        {
            WebCamDevice[] devices = WebCamTexture.devices;
            var choices = new List<string> { "Default Camera" };
            if (devices == null || devices.Length == 0)
                return choices;

            for (int i = 0; i < devices.Length; i++)
                choices.Add(devices[i].name);

            return choices;
        }

        private string ResolveCameraDropdownValue(List<string> choices)
        {
            string deviceName = ResolveDeviceName();
            if (!string.IsNullOrWhiteSpace(deviceName) && choices.Contains(deviceName))
                return deviceName;

            return choices[0];
        }

        private void SelectCamera(string selected)
        {
            if (string.IsNullOrWhiteSpace(selected))
                return;

            deviceNameOverride = selected == "Default Camera" ? null : selected;
            WebCamDevice[] devices = WebCamTexture.devices;
            if (devices != null)
            {
                for (int i = 0; i < devices.Length; i++)
                {
                    if (devices[i].name == deviceNameOverride)
                    {
                        deviceIndex = i;
                        break;
                    }
                }
            }

            RestartWebCamPreview();
            UpdateManagerCameraSettings();
        }

        private void RotatePreview()
        {
            previewRotationDegrees = (previewRotationDegrees + 90) % 360;
            ApplyDashboardPreviewSettings();
        }

        private void ToggleMirror()
        {
            mirrorPreviewX = !mirrorPreviewX;
            ApplyDashboardPreviewSettings();
        }

        private void UpdateManagerCameraSettings()
        {
            if (manager == null)
                return;

            manager.webCamDeviceName = ResolveDeviceName();
            manager.webCamRequestedWidth = requestedWidth;
            manager.webCamRequestedHeight = requestedHeight;
            manager.webCamRequestedFps = requestedFps;
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
