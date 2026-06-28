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
        public UniversalTrackerManager trackerManager;
        public bool autoFindManager = true;
        public bool subscribeToManagerEvent = true;

        [Header("Dashboard")]
        [SerializeField] private bool isReceiverEnabled = true;
        public bool showPreview = true;
        public bool showDetections = true;
        public bool showPoses = true;
        public bool showMasks = true;
        public bool showStats = true;
        public bool showOverlayMetrics = true;
        [Range(0.05f, 1f)] public float keypointConfidenceThreshold = 0.35f;
        [Range(0.05f, 0.8f)] public float maskAlpha = 0.28f;
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
            ReceiveVisionResult(result, lastTexture);
        }

        public void ReceiveVisionResult(VisionFrameResult result, Texture sourceTexture)
        {
            if (!isReceiverEnabled || result == null)
                return;

            EnsureBuilt();
            lastTexture = sourceTexture ?? lastTexture;
            UpdatePreview(lastTexture);
            statsBinder.UpdateStats(result);
            UpdateOverlay(result);
            resultListBinder.UpdateRows(result, CreateResultListSettings());
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
                () => trackerManager?.StartTracking(),
                () => trackerManager?.StopTracking());

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
        }

        private void UpdateOverlay(VisionFrameResult result)
        {
            if (overlay == null || previewStage == null)
                return;

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
                keypointConfidenceThreshold);
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
    }
}
