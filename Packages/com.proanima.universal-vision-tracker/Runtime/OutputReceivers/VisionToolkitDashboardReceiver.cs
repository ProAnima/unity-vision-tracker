using System;
using UnityEngine;
using UnityEngine.UIElements;
using UniversalTracker.Core;
using UniversalTracker.Visualization;

namespace UniversalTracker.OutputReceivers
{
    /// <summary>
    /// Runtime UI Toolkit dashboard driven by the canonical VisionFrameResult API.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class VisionToolkitDashboardReceiver : MonoBehaviour, IOutputReceiver, IVisionFrameResultReceiver
    {
        [Header("Data Source")]
        [Tooltip("Tracker manager that feeds this dashboard.")]
        public UniversalTrackerManager trackerManager;
        [Tooltip("Find the first UniversalTrackerManager automatically when no manager is assigned.")]
        public bool autoFindManager = true;
        [Tooltip("Subscribe to the manager event stream instead of relying only on manual receiver dispatch.")]
        public bool subscribeToManagerEvent = true;

        [Header("Dashboard")]
        [SerializeField, Tooltip("Show and update the dashboard.")]
        private bool isReceiverEnabled = true;
        [Tooltip("Show the source frame preview.")]
        public bool showPreview = true;
        [Tooltip("Show all enabled overlays on top of the preview.")]
        public bool showVisualization = true;
        [Tooltip("Draw detection bounding boxes and labels.")]
        public bool showDetections = true;
        [Tooltip("Draw pose keypoints and skeleton bones.")]
        public bool showPoses = true;
        [Tooltip("Draw segmentation mask fills and contours.")]
        public bool showMasks = true;
        [Tooltip("Show runtime status, frame counters, and result counts.")]
        public bool showStats = true;
        [Tooltip("Show source, viewport, and fit metrics in the overlay corner.")]
        public bool showOverlayMetrics = true;
        [Tooltip("How the source preview fits inside the dashboard viewport.")]
        public ScaleMode previewScaleMode = ScaleMode.ScaleToFit;
        [Tooltip("Clockwise preview rotation in degrees.")]
        public int previewRotationDegrees;
        [Tooltip("Mirror the preview horizontally.")]
        public bool mirrorPreviewX;
        [Tooltip("Mirror the preview vertically.")]
        public bool mirrorPreviewY;
        [Tooltip("Minimum keypoint confidence required for pose drawing.")]
        [Range(0.05f, 1f)] public float keypointConfidenceThreshold = 0.35f;
        [Tooltip("Temporal smoothing amount for overlay boxes, contours, pose bones, and keypoints.")]
        [Range(0f, 0.95f)] public float poseSmoothing = 0.55f;
        [Tooltip("Opacity of segmentation mask fills.")]
        [Range(0.05f, 0.8f)] public float maskAlpha = 0.28f;
        [Tooltip("Maximum result rows displayed in the side panel.")]
        [Range(1, 20)] public int maxRows = 8;

        public bool IsEnabled
        {
            get => isReceiverEnabled;
            set
            {
                isReceiverEnabled = value;
                if (root != null)
                    root.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private UIDocument document;
        private VisualElement root;
        private VisualElement previewStage;
        private Image previewImage;
        private VisualElement overlay;
        private VisualElement contentGuide;
        private Label overlayMetricsLabel;
        private bool isSubscribed;
        private Texture lastTexture;

        private VisionToolkitDashboardStatsBinder statsBinder;
        private readonly VisionToolkitDashboardResultListBinder resultListBinder = new VisionToolkitDashboardResultListBinder();
        private readonly VisionDashboardOverlayState overlayState = new VisionDashboardOverlayState();
        private Action startCommand;
        private Action stopCommand;

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            if (root != null)
                root.style.display = isReceiverEnabled ? DisplayStyle.Flex : DisplayStyle.None;

            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Release();
        }

        public void Initialize()
        {
            EnsureBinders();
            document = document != null ? document : GetComponent<UIDocument>();
            if (document == null)
            {
                Debug.LogWarning("[VisionDashboard] UIDocument is required for UI Toolkit dashboard.");
                isReceiverEnabled = false;
                return;
            }

            BuildInterface();
            Subscribe();
        }

        public void ReceiveVisionResult(VisionFrameResult result)
        {
            ReceiveVisionResult(result, result?.sourceTexture ?? lastTexture);
        }

        public void ReceiveVisionResult(VisionFrameResult result, Texture sourceTexture)
        {
            if (!isReceiverEnabled || result == null)
                return;

            EnsureBuilt();
            lastTexture = sourceTexture ?? result.sourceTexture ?? lastTexture;
            UpdatePreview(lastTexture);
            statsBinder.UpdateStats(result);
            UpdateOverlay(result);
            resultListBinder.UpdateRows(result, CreateResultListSettings());
        }

        public void SetCommandHandlers(Action start, Action stop)
        {
            startCommand = start;
            stopCommand = stop;
        }

        public void SetHealthStatus(VisionHealthStatus status)
        {
            EnsureBuilt();
            statsBinder.UpdateHealth(status);
        }

        public void Clear()
        {
            VisionToolkitDashboardOverlayRenderer.Clear(overlayState);
            resultListBinder.Clear();

            if (previewImage != null)
                previewImage.image = null;
        }

        public void Release()
        {
            Unsubscribe();
            Clear();
        }

        private void Subscribe()
        {
            EnsureBinders();
            if (!subscribeToManagerEvent || isSubscribed)
                return;

            if (trackerManager == null && autoFindManager)
                trackerManager = FindFirstObjectByType<UniversalTrackerManager>();

            if (trackerManager == null)
                return;

            trackerManager.OnVisionFrameResult += ReceiveVisionResult;
            trackerManager.OnVisionHealthChanged += HandleHealthChanged;
            isSubscribed = true;
            statsBinder.UpdateHealth(trackerManager.HealthStatus);
        }

        private void Unsubscribe()
        {
            if (!isSubscribed || trackerManager == null)
                return;

            trackerManager.OnVisionFrameResult -= ReceiveVisionResult;
            trackerManager.OnVisionHealthChanged -= HandleHealthChanged;
            isSubscribed = false;
        }

        private void EnsureBuilt()
        {
            if (root == null)
                BuildInterface();
        }

        private void BuildInterface()
        {
            EnsureBinders();
            VisionToolkitDashboardView view = VisionToolkitDashboardViewBuilder.Build(
                document,
                isReceiverEnabled,
                StartFromDashboard,
                StopFromDashboard);

            if (view == null)
                return;

            root = view.root;
            previewStage = view.previewStage;
            previewImage = view.previewImage;
            overlay = view.overlay;
            contentGuide = view.contentGuide;
            overlayState.maskLayer = view.maskLayer;
            overlayState.detectionLayer = view.detectionLayer;
            overlayState.boneLayer = view.boneLayer;
            overlayState.keypointLayer = view.keypointLayer;
            overlayState.labelLayer = view.labelLayer;
            overlayMetricsLabel = view.overlayMetricsLabel;
            statsBinder.Bind(view);
            resultListBinder.Bind(view.list);
            BindControls(view);
        }

        private void EnsureBinders()
        {
            statsBinder ??= new VisionToolkitDashboardStatsBinder(() => trackerManager);
        }

        private void HandleHealthChanged(VisionHealthStatus status)
        {
            statsBinder.UpdateHealth(status);
        }

        private void UpdatePreview(Texture sourceTexture)
        {
            if (previewImage == null)
                return;

            previewImage.style.display = showPreview && sourceTexture != null ? DisplayStyle.Flex : DisplayStyle.None;
            previewImage.image = sourceTexture;
            previewImage.scaleMode = previewScaleMode;
            previewImage.transform.rotation = Quaternion.Euler(0f, 0f, previewRotationDegrees);
            previewImage.transform.scale = new Vector3(mirrorPreviewX ? -1f : 1f, mirrorPreviewY ? -1f : 1f, 1f);
        }

        private void UpdateOverlay(VisionFrameResult result)
        {
            if (overlay == null || previewStage == null)
                return;

            if (!showVisualization)
            {
                VisionToolkitDashboardOverlayRenderer.Clear(overlayState);
                if (overlayMetricsLabel != null)
                    overlayMetricsLabel.style.display = DisplayStyle.None;
                return;
            }

            Vector2 viewportSize = previewStage.layout.size;
            Vector2 sourceSize = ResolveSourceSize(result);
            if (viewportSize.x <= 1f || viewportSize.y <= 1f || sourceSize.x <= 0f || sourceSize.y <= 0f)
                return;

            Rect contentRect = VisionDashboardGeometry.CalculateScaleToFitRect(sourceSize, viewportSize);
            float stroke = VisionDashboardGeometry.CalculateAdaptiveStroke(viewportSize);
            UpdateContentGuide(contentRect);
            UpdateOverlayMetrics(result, sourceSize, viewportSize, contentRect);

            VisionToolkitDashboardOverlayRenderer.Render(
                result,
                overlayState,
                sourceSize,
                viewportSize,
                stroke,
                showMasks,
                showDetections,
                showPoses,
                maskAlpha,
                keypointConfidenceThreshold,
                poseSmoothing);
        }

        private void UpdateContentGuide(Rect contentRect)
        {
            if (contentGuide == null)
                return;

            contentGuide.style.left = contentRect.x;
            contentGuide.style.top = contentRect.y;
            contentGuide.style.width = contentRect.width;
            contentGuide.style.height = contentRect.height;
        }

        private void UpdateOverlayMetrics(VisionFrameResult result, Vector2 sourceSize, Vector2 viewportSize, Rect contentRect)
        {
            if (overlayMetricsLabel == null)
                return;

            overlayMetricsLabel.style.display = showOverlayMetrics ? DisplayStyle.Flex : DisplayStyle.None;
            if (!showOverlayMetrics)
                return;

            int detections = result.detections?.Length ?? 0;
            int poses = result.poses?.Length ?? 0;
            int masks = result.masks?.Length ?? 0;
            overlayMetricsLabel.text =
                $"src {sourceSize.x:F0}x{sourceSize.y:F0} | view {viewportSize.x:F0}x{viewportSize.y:F0}\n" +
                $"fit {contentRect.width:F0}x{contentRect.height:F0} | d:{detections} p:{poses} m:{masks}";
        }

        private Vector2 ResolveSourceSize(VisionFrameResult result)
        {
            if (result.sourceSize.x > 0 && result.sourceSize.y > 0)
                return result.sourceSize;

            if (lastTexture != null)
                return new Vector2(lastTexture.width, lastTexture.height);

            return new Vector2(1f, 1f);
        }

        private VisionDashboardResultListSettings CreateResultListSettings()
        {
            return new VisionDashboardResultListSettings(showDetections, showPoses, showMasks, maxRows);
        }

        private void BindControls(VisionToolkitDashboardView view)
        {
            if (view.visualizationToggle != null)
            {
                view.visualizationToggle.SetValueWithoutNotify(showVisualization);
                view.visualizationToggle.RegisterValueChangedCallback(evt => showVisualization = evt.newValue);
            }

            BindToggle(view.detectionsToggle, showDetections, value => showDetections = value);
            BindToggle(view.posesToggle, showPoses, value => showPoses = value);
            BindToggle(view.masksToggle, showMasks, value => showMasks = value);
            BindSlider(view.keypointSlider, keypointConfidenceThreshold, value => keypointConfidenceThreshold = value);
            BindSlider(view.poseSmoothingSlider, poseSmoothing, value => poseSmoothing = value);
            BindSlider(view.maskAlphaSlider, maskAlpha, value => maskAlpha = value);
            BindSlider(view.targetFpsSlider, ResolveTargetFps(), value => trackerManager?.SetTargetFps(Mathf.RoundToInt(value)));
            BindSlider(view.confidenceSlider, ResolveConfidenceThreshold(), value => ApplyModelThresholds(value, ResolveNmsThreshold()));
            BindSlider(view.nmsSlider, ResolveNmsThreshold(), value => ApplyModelThresholds(ResolveConfidenceThreshold(), value));
        }

        private static void BindToggle(Toggle toggle, bool value, Action<bool> changed)
        {
            if (toggle == null)
                return;

            toggle.SetValueWithoutNotify(value);
            toggle.RegisterValueChangedCallback(evt => changed(evt.newValue));
        }

        private static void BindSlider(Slider slider, float value, Action<float> changed)
        {
            if (slider == null)
                return;

            slider.SetValueWithoutNotify(value);
            slider.RegisterValueChangedCallback(evt => changed(evt.newValue));
        }

        private int ResolveTargetFps()
        {
            return trackerManager != null ? trackerManager.targetFPS : 30;
        }

        private float ResolveConfidenceThreshold()
        {
            return ResolveActiveProfile() != null ? ResolveActiveProfile().confidenceThreshold : 0.25f;
        }

        private float ResolveNmsThreshold()
        {
            return ResolveActiveProfile() != null ? ResolveActiveProfile().nmsThreshold : 0.45f;
        }

        private VisionModelProfile ResolveActiveProfile()
        {
            if (trackerManager == null)
                return null;

            return trackerManager.ActiveModelProfile ??
                   VisionModelProfileResolver.Resolve(trackerManager.pipelineProfile, trackerManager.modelProfiles, ref trackerManager.activeModelIndex);
        }

        private void ApplyModelThresholds(float confidenceThreshold, float nmsThreshold)
        {
            trackerManager?.SetModelThresholds(confidenceThreshold, nmsThreshold);
        }

        private void StartFromDashboard()
        {
            if (startCommand != null)
            {
                startCommand.Invoke();
                return;
            }

            trackerManager?.StartTracking();
        }

        private void StopFromDashboard()
        {
            if (stopCommand != null)
            {
                stopCommand.Invoke();
                return;
            }

            trackerManager?.StopTracking();
        }
    }
}
