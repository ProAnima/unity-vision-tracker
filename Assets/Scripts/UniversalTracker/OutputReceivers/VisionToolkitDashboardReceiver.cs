using System.Collections.Generic;
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
        private VisualElement maskLayer;
        private VisualElement detectionLayer;
        private VisualElement boneLayer;
        private VisualElement keypointLayer;
        private VisualElement labelLayer;
        private VisualElement list;
        private Label overlayMetricsLabel;
        private Label statusLabel;
        private Label frameLabel;
        private Label fpsLabel;
        private Label inferenceLabel;
        private Label detectionCountLabel;
        private Label poseCountLabel;
        private Label errorLabel;
        private Button startButton;
        private Button stopButton;
        private bool isSubscribed;
        private Texture lastTexture;

        private readonly VisionDashboardOverlayState overlayState = new VisionDashboardOverlayState();
        private readonly List<Label> rowPool = new List<Label>();

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
            UpdateStats(result);
            UpdateOverlay(result);
            UpdateRows(result);
        }

        public void Clear()
        {
            VisionToolkitDashboardOverlayRenderer.Clear(overlayState);
            SetRowsActive(0);

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
            if (!subscribeToManagerEvent || isSubscribed)
                return;

            if (trackerManager == null && autoFindManager)
                trackerManager = FindFirstObjectByType<UniversalTrackerManager>();

            if (trackerManager == null)
                return;

            trackerManager.OnVisionFrameResult += ReceiveVisionResult;
            trackerManager.OnVisionHealthChanged += HandleHealthChanged;
            isSubscribed = true;
            UpdateHealthStatus(trackerManager.HealthStatus);
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
            maskLayer = view.maskLayer;
            detectionLayer = view.detectionLayer;
            boneLayer = view.boneLayer;
            keypointLayer = view.keypointLayer;
            labelLayer = view.labelLayer;
            overlayState.maskLayer = maskLayer;
            overlayState.detectionLayer = detectionLayer;
            overlayState.boneLayer = boneLayer;
            overlayState.keypointLayer = keypointLayer;
            overlayState.labelLayer = labelLayer;
            list = view.list;
            overlayMetricsLabel = view.overlayMetricsLabel;
            statusLabel = view.statusLabel;
            frameLabel = view.frameLabel;
            fpsLabel = view.fpsLabel;
            inferenceLabel = view.inferenceLabel;
            detectionCountLabel = view.detectionCountLabel;
            poseCountLabel = view.poseCountLabel;
            errorLabel = view.errorLabel;
            startButton = view.startButton;
            stopButton = view.stopButton;
        }

        private void UpdatePreview(Texture sourceTexture)
        {
            if (previewImage == null)
                return;

            previewImage.style.display = showPreview && sourceTexture != null ? DisplayStyle.Flex : DisplayStyle.None;
            previewImage.image = sourceTexture;
        }

        private void UpdateStats(VisionFrameResult result)
        {
            UpdateHealthStatus(trackerManager != null ? trackerManager.HealthStatus : null);

            frameLabel.text = result.frameIndex.ToString();
            fpsLabel.text = trackerManager != null ? trackerManager.CurrentFPS.ToString("F1") : "-";
            inferenceLabel.text = result.stats.inferenceMs > 0f ? $"{result.stats.inferenceMs:F1} ms" : "-";
            detectionCountLabel.text = (result.detections?.Length ?? 0).ToString();
            poseCountLabel.text = (result.poses?.Length ?? 0).ToString();
            errorLabel.text = trackerManager != null ? trackerManager.ConsecutiveErrors.ToString() : "0";
        }

        private void HandleHealthChanged(VisionHealthStatus status)
        {
            UpdateHealthStatus(status);
        }

        private void UpdateHealthStatus(VisionHealthStatus status)
        {
            if (statusLabel == null)
                return;

            VisionHealthState state = status?.state ?? VisionHealthState.NotInitialized;
            statusLabel.text = state.ToString();
            VisionDashboardTheme.SetPillColor(statusLabel, VisionDashboardTheme.HealthColor(state));

            if (errorLabel != null)
                errorLabel.text = trackerManager != null ? trackerManager.ConsecutiveErrors.ToString() : "0";
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

        private void UpdateRows(VisionFrameResult result)
        {
            int used = 0;
            if (showDetections && result.detections != null)
            {
                for (int i = 0; i < result.detections.Length && used < maxRows; i++)
                {
                    VisionDetection detection = result.detections[i];
                    string name = string.IsNullOrWhiteSpace(detection.label) ? $"Class {detection.classId}" : detection.label;
                    string track = detection.IsTracked ? $" T{detection.trackId}" : string.Empty;
                    UpdateRow(GetRow(used), $"{name}{track}", detection.confidence, VisionDashboardTheme.Accent);
                    used++;
                }
            }

            if (showPoses && result.poses != null)
            {
                for (int i = 0; i < result.poses.Length && used < maxRows; i++)
                {
                    VisionPose pose = result.poses[i];
                    string name = pose.personId >= 0 ? $"Pose T{pose.personId}" : "Pose";
                    UpdateRow(GetRow(used), $"{name} · {pose.VisibleKeypointCount} pts", pose.confidence, VisionDashboardTheme.PoseColor);
                    used++;
                }
            }

            if (showMasks && result.masks != null)
            {
                for (int i = 0; i < result.masks.Length && used < maxRows; i++)
                {
                    VisionMask mask = result.masks[i];
                    string name = string.IsNullOrWhiteSpace(mask.label) ? $"Mask {mask.classId}" : mask.label;
                    string track = mask.trackId >= 0 ? $" T{mask.trackId}" : string.Empty;
                    UpdateRow(GetRow(used), $"{name}{track}", mask.confidence, VisionDashboardTheme.Warning);
                    used++;
                }
            }

            if (used == 0)
            {
                Label empty = GetRow(0);
                empty.text = "No active results";
                empty.style.color = VisionDashboardTheme.MutedText;
                empty.style.backgroundColor = VisionDashboardTheme.PanelSoft;
                used = 1;
            }

            SetRowsActive(used);
        }

        private Vector2 ResolveSourceSize(VisionFrameResult result)
        {
            if (result.sourceSize.x > 0 && result.sourceSize.y > 0)
                return result.sourceSize;

            if (lastTexture != null)
                return new Vector2(lastTexture.width, lastTexture.height);

            return new Vector2(1f, 1f);
        }

        private Label GetRow(int index)
        {
            while (rowPool.Count <= index)
            {
                Label row = VisionToolkitDashboardViewBuilder.CreateResultRow(list);
                rowPool.Add(row);
            }

            rowPool[index].style.display = DisplayStyle.Flex;
            return rowPool[index];
        }

        private void UpdateRow(Label row, string label, float confidence, Color color)
        {
            row.text = $"{label}  {(confidence * 100f):F0}%";
            row.style.color = VisionDashboardTheme.Text;
            row.style.backgroundColor = new Color(color.r, color.g, color.b, 0.14f);
        }

        private void SetRowsActive(int activeCount)
        {
            for (int i = 0; i < rowPool.Count; i++)
                rowPool[i].style.display = i < activeCount ? DisplayStyle.Flex : DisplayStyle.None;
        }

    }
}
