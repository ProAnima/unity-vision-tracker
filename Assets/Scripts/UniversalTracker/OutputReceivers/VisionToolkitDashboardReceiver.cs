using System;
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
    public sealed class VisionToolkitDashboardReceiver : MonoBehaviour, IOutputReceiver
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

        private static readonly Color Panel = new Color(0.06f, 0.08f, 0.1f, 0.92f);
        private static readonly Color PanelSoft = new Color(0.09f, 0.12f, 0.15f, 0.92f);
        private static readonly Color Border = new Color(0.26f, 0.34f, 0.4f, 0.7f);
        private static readonly Color Text = new Color(0.9f, 0.96f, 1f, 1f);
        private static readonly Color MutedText = new Color(0.58f, 0.68f, 0.75f, 1f);
        private static readonly Color Accent = new Color(0.16f, 0.78f, 0.98f, 1f);
        private static readonly Color Good = new Color(0.38f, 0.95f, 0.55f, 1f);
        private static readonly Color Warning = new Color(1f, 0.72f, 0.28f, 1f);
        private static readonly Color PoseColor = new Color(0.88f, 0.96f, 0.45f, 1f);

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

        private readonly List<VisualElement> maskPool = new List<VisualElement>();
        private readonly List<VisualElement> detectionPool = new List<VisualElement>();
        private readonly List<Label> detectionLabelPool = new List<Label>();
        private readonly List<VisualElement> keypointPool = new List<VisualElement>();
        private readonly List<VisualElement> bonePool = new List<VisualElement>();
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

        public void ReceiveResult(InferenceResult result, Texture sourceTexture)
        {
            if (!isReceiverEnabled || result == null)
                return;

            ReceiveVisionResult(VisionResultAdapter.FromInferenceResult(result, sourceTexture), sourceTexture);
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
            SetPoolActive(maskPool, 0);
            SetPoolActive(detectionPool, 0);
            SetLabelsActive(detectionLabelPool, 0);
            SetPoolActive(keypointPool, 0);
            SetPoolActive(bonePool, 0);
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
            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!isSubscribed || trackerManager == null)
                return;

            trackerManager.OnVisionFrameResult -= ReceiveVisionResult;
            isSubscribed = false;
        }

        private void EnsureBuilt()
        {
            if (root == null)
                BuildInterface();
        }

        private void BuildInterface()
        {
            if (document == null || document.rootVisualElement == null)
                return;

            root = document.rootVisualElement;
            root.Clear();
            root.style.flexGrow = 1f;
            root.style.flexDirection = FlexDirection.Row;
            root.style.paddingLeft = 18;
            root.style.paddingRight = 18;
            root.style.paddingTop = 18;
            root.style.paddingBottom = 18;
            root.style.backgroundColor = new Color(0.015f, 0.02f, 0.024f, 0.94f);
            root.style.display = isReceiverEnabled ? DisplayStyle.Flex : DisplayStyle.None;

            var side = CreatePanel(320, 0);
            side.style.marginRight = 14;
            side.style.flexShrink = 0f;
            root.Add(side);

            AddHeader(side);
            AddControls(side);
            AddStats(side);
            AddResultList(side);

            previewStage = new VisualElement { name = "VisionPreviewStage" };
            previewStage.style.flexGrow = 1f;
            previewStage.style.minWidth = 320;
            previewStage.style.backgroundColor = new Color(0.02f, 0.026f, 0.032f, 1f);
            previewStage.style.borderTopLeftRadius = 8;
            previewStage.style.borderTopRightRadius = 8;
            previewStage.style.borderBottomLeftRadius = 8;
            previewStage.style.borderBottomRightRadius = 8;
            previewStage.style.borderTopWidth = 1;
            previewStage.style.borderRightWidth = 1;
            previewStage.style.borderBottomWidth = 1;
            previewStage.style.borderLeftWidth = 1;
            previewStage.style.borderTopColor = Border;
            previewStage.style.borderRightColor = Border;
            previewStage.style.borderBottomColor = Border;
            previewStage.style.borderLeftColor = Border;
            root.Add(previewStage);

            previewImage = new Image { name = "VisionPreviewImage", scaleMode = ScaleMode.ScaleToFit };
            previewImage.style.position = Position.Absolute;
            previewImage.style.left = 0;
            previewImage.style.right = 0;
            previewImage.style.top = 0;
            previewImage.style.bottom = 0;
            previewStage.Add(previewImage);

            overlay = new VisualElement { name = "VisionOverlay" };
            overlay.pickingMode = PickingMode.Ignore;
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0;
            overlay.style.right = 0;
            overlay.style.top = 0;
            overlay.style.bottom = 0;
            previewStage.Add(overlay);

            contentGuide = new VisualElement { name = "VisionContentGuide" };
            contentGuide.pickingMode = PickingMode.Ignore;
            contentGuide.style.position = Position.Absolute;
            contentGuide.style.borderTopWidth = 1;
            contentGuide.style.borderRightWidth = 1;
            contentGuide.style.borderBottomWidth = 1;
            contentGuide.style.borderLeftWidth = 1;
            contentGuide.style.borderTopColor = new Color(1f, 1f, 1f, 0.18f);
            contentGuide.style.borderRightColor = new Color(1f, 1f, 1f, 0.18f);
            contentGuide.style.borderBottomColor = new Color(1f, 1f, 1f, 0.18f);
            contentGuide.style.borderLeftColor = new Color(1f, 1f, 1f, 0.18f);
            overlay.Add(contentGuide);

            maskLayer = CreateOverlayLayer("Masks");
            boneLayer = CreateOverlayLayer("Bones");
            detectionLayer = CreateOverlayLayer("Detections");
            keypointLayer = CreateOverlayLayer("Keypoints");
            labelLayer = CreateOverlayLayer("Labels");
            overlay.Add(maskLayer);
            overlay.Add(boneLayer);
            overlay.Add(detectionLayer);
            overlay.Add(keypointLayer);
            overlay.Add(labelLayer);

            overlayMetricsLabel = new Label();
            overlayMetricsLabel.pickingMode = PickingMode.Ignore;
            overlayMetricsLabel.style.position = Position.Absolute;
            overlayMetricsLabel.style.right = 10;
            overlayMetricsLabel.style.top = 10;
            overlayMetricsLabel.style.paddingLeft = 8;
            overlayMetricsLabel.style.paddingRight = 8;
            overlayMetricsLabel.style.paddingTop = 5;
            overlayMetricsLabel.style.paddingBottom = 5;
            overlayMetricsLabel.style.borderTopLeftRadius = 6;
            overlayMetricsLabel.style.borderTopRightRadius = 6;
            overlayMetricsLabel.style.borderBottomLeftRadius = 6;
            overlayMetricsLabel.style.borderBottomRightRadius = 6;
            overlayMetricsLabel.style.backgroundColor = new Color(0f, 0f, 0f, 0.52f);
            overlayMetricsLabel.style.color = Text;
            overlayMetricsLabel.style.fontSize = 11;
            overlay.Add(overlayMetricsLabel);
        }

        private void AddHeader(VisualElement parent)
        {
            var title = new Label("ProAnima Vision");
            title.style.fontSize = 24;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Text;
            title.style.marginBottom = 4;
            parent.Add(title);

            statusLabel = CreatePill("Standing by", Warning);
            statusLabel.style.alignSelf = Align.FlexStart;
            statusLabel.style.marginBottom = 14;
            parent.Add(statusLabel);
        }

        private void AddControls(VisualElement parent)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 14;
            parent.Add(row);

            startButton = CreateCommandButton("Start", Good);
            startButton.clicked += () => trackerManager?.StartTracking();
            row.Add(startButton);

            stopButton = CreateCommandButton("Stop", Warning);
            stopButton.clicked += () => trackerManager?.StopTracking();
            stopButton.style.marginLeft = 8;
            row.Add(stopButton);
        }

        private void AddStats(VisualElement parent)
        {
            var stats = CreatePanel(0, 0);
            stats.style.marginBottom = 14;
            parent.Add(stats);

            frameLabel = AddStat(stats, "Frame", "-");
            fpsLabel = AddStat(stats, "Runtime FPS", "-");
            inferenceLabel = AddStat(stats, "Inference", "-");
            detectionCountLabel = AddStat(stats, "Detections", "0");
            poseCountLabel = AddStat(stats, "Poses", "0");
            errorLabel = AddStat(stats, "Errors", "0");
        }

        private void AddResultList(VisualElement parent)
        {
            var caption = new Label("Live Results");
            caption.style.fontSize = 12;
            caption.style.color = MutedText;
            caption.style.marginBottom = 6;
            parent.Add(caption);

            list = new VisualElement();
            list.style.flexGrow = 1f;
            parent.Add(list);
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
            bool running = trackerManager != null && trackerManager.IsRunning;
            statusLabel.text = running ? "Running" : "Ready";
            SetPillColor(statusLabel, running ? Good : Warning);

            frameLabel.text = result.frameIndex.ToString();
            fpsLabel.text = trackerManager != null ? trackerManager.CurrentFPS.ToString("F1") : "-";
            inferenceLabel.text = result.stats.inferenceMs > 0f ? $"{result.stats.inferenceMs:F1} ms" : "-";
            detectionCountLabel.text = (result.detections?.Length ?? 0).ToString();
            poseCountLabel.text = (result.poses?.Length ?? 0).ToString();
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

            int masksUsed = 0;
            if (showMasks && result.masks != null)
            {
                for (int i = 0; i < result.masks.Length; i++)
                    UpdateMask(GetElement(maskPool, maskLayer, CreateMaskOverlay, masksUsed), masksUsed++, result.masks[i], sourceSize, viewportSize, stroke);
            }

            int detectionsUsed = 0;
            int detectionLabelsUsed = 0;
            if (showDetections && result.detections != null)
            {
                for (int i = 0; i < result.detections.Length; i++)
                    UpdateDetectionBox(
                        GetElement(detectionPool, detectionLayer, CreateDetectionBox, detectionsUsed),
                        GetLabel(detectionLabelPool, labelLayer, detectionLabelsUsed++),
                        detectionsUsed++,
                        result.detections[i],
                        sourceSize,
                        viewportSize,
                        stroke);
            }

            int keypointsUsed = 0;
            int bonesUsed = 0;
            if (showPoses && result.poses != null)
            {
                for (int i = 0; i < result.poses.Length; i++)
                    UpdatePose(result.poses[i], sourceSize, viewportSize, stroke, ref keypointsUsed, ref bonesUsed);
            }

            SetPoolActive(maskPool, masksUsed);
            SetPoolActive(detectionPool, detectionsUsed);
            SetLabelsActive(detectionLabelPool, detectionLabelsUsed);
            SetPoolActive(keypointPool, keypointsUsed);
            SetPoolActive(bonePool, bonesUsed);
        }

        private void UpdateDetectionBox(VisualElement box, Label label, int index, VisionDetection detection, Vector2 sourceSize, Vector2 viewportSize, float stroke)
        {
            Rect rect = VisionDashboardGeometry.NormalizedToViewportRect(detection.normalizedRect, sourceSize, viewportSize);
            box.style.left = rect.x;
            box.style.top = rect.y;
            box.style.width = Mathf.Max(1f, rect.width);
            box.style.height = Mathf.Max(1f, rect.height);
            box.style.borderTopWidth = stroke;
            box.style.borderRightWidth = stroke;
            box.style.borderBottomWidth = stroke;
            box.style.borderLeftWidth = stroke;

            string name = string.IsNullOrWhiteSpace(detection.label) ? $"#{detection.classId}" : detection.label;
            string id = detection.IsTracked ? $" T{detection.trackId}" : string.Empty;
            label.text = $"{name} {(detection.confidence * 100f):F0}%{id}";

            int stableId = detection.IsTracked ? detection.trackId : detection.classId + index * 31;
            Color color = VisionDashboardGeometry.StableColor(stableId);
            SetBorderColor(box, color);
            box.style.backgroundColor = new Color(color.r, color.g, color.b, 0.06f);

            Vector2 labelSize = new Vector2(Mathf.Clamp(label.text.Length * 7.5f + 18f, 86f, 260f), 24f);
            Vector2 labelPosition = VisionDashboardGeometry.ClampLabelPosition(rect, labelSize, viewportSize);
            label.style.left = labelPosition.x;
            label.style.top = labelPosition.y;
            label.style.width = labelSize.x;
            label.style.height = labelSize.y;
            label.style.backgroundColor = new Color(color.r, color.g, color.b, 0.9f);
            label.style.color = Color.black;
        }

        private void UpdatePose(VisionPose pose, Vector2 sourceSize, Vector2 viewportSize, float stroke, ref int keypointsUsed, ref int bonesUsed)
        {
            if (pose.keypoints == null)
                return;

            if (pose.skeleton.bones != null)
            {
                for (int i = 0; i < pose.skeleton.bones.Length; i++)
                {
                    VisionSkeletonBone bone = pose.skeleton.bones[i];
                    if (!TryGetVisibleKeypoint(pose, bone.from, out VisionKeypoint from) ||
                        !TryGetVisibleKeypoint(pose, bone.to, out VisionKeypoint to))
                    {
                        continue;
                    }

                    Vector2 fromPoint = VisionDashboardGeometry.NormalizedToViewportPoint(from.normalizedPosition, sourceSize, viewportSize);
                    Vector2 toPoint = VisionDashboardGeometry.NormalizedToViewportPoint(to.normalizedPosition, sourceSize, viewportSize);
                    UpdateBone(GetElement(bonePool, boneLayer, CreateBone, bonesUsed), bonesUsed++, fromPoint, toPoint, from.confidence, to.confidence, stroke);
                }
            }

            for (int i = 0; i < pose.keypoints.Length; i++)
            {
                if (!IsVisibleKeypoint(pose.keypoints[i]))
                    continue;

                Vector2 point = VisionDashboardGeometry.NormalizedToViewportPoint(pose.keypoints[i].normalizedPosition, sourceSize, viewportSize);
                UpdateKeypoint(GetElement(keypointPool, keypointLayer, CreateKeypoint, keypointsUsed), keypointsUsed++, point, pose.keypoints[i], stroke);
            }
        }

        private void UpdateBone(VisualElement bone, int index, Vector2 from, Vector2 to, float fromConfidence, float toConfidence, float stroke)
        {
            BoneLine line = VisionDashboardGeometry.CalculateBoneLine(from, to);
            float confidence = Mathf.Clamp01(Mathf.Min(fromConfidence, toConfidence));
            bone.style.left = line.center.x - line.length * 0.5f;
            bone.style.top = line.center.y - stroke * 0.5f;
            bone.style.width = Mathf.Max(1f, line.length);
            bone.style.height = Mathf.Max(2f, stroke);
            bone.style.rotate = new Rotate(new Angle(line.angleDegrees, AngleUnit.Degree));
            Color color = index % 2 == 0 ? PoseColor : Accent;
            bone.style.backgroundColor = new Color(color.r, color.g, color.b, Mathf.Lerp(0.38f, 0.95f, confidence));
        }

        private void UpdateKeypoint(VisualElement keypoint, int index, Vector2 point, VisionKeypoint data, float stroke)
        {
            float radius = Mathf.Clamp(stroke * 2.2f, 4f, 8f);
            keypoint.style.left = point.x - radius;
            keypoint.style.top = point.y - radius;
            keypoint.style.width = radius * 2f;
            keypoint.style.height = radius * 2f;
            keypoint.style.backgroundColor = data.confidence >= 0.5f ? PoseColor : Warning;
            SetBorderColor(keypoint, new Color(0f, 0f, 0f, 0.8f));
        }

        private void UpdateMask(VisualElement element, int index, VisionMask mask, Vector2 sourceSize, Vector2 viewportSize, float stroke)
        {
            Rect rect = VisionDashboardGeometry.NormalizedToViewportRect(mask.normalizedRect, sourceSize, viewportSize);
            element.style.left = rect.x;
            element.style.top = rect.y;
            element.style.width = Mathf.Max(1f, rect.width);
            element.style.height = Mathf.Max(1f, rect.height);
            element.style.borderTopWidth = stroke;
            element.style.borderRightWidth = stroke;
            element.style.borderBottomWidth = stroke;
            element.style.borderLeftWidth = stroke;

            int stableId = mask.trackId >= 0 ? mask.trackId : mask.classId + index * 37;
            Color color = VisionDashboardGeometry.StableColor(stableId, 0.68f, 0.88f);
            SetBorderColor(element, new Color(color.r, color.g, color.b, 0.95f));
            element.style.backgroundColor = new Color(color.r, color.g, color.b, maskAlpha);

            var image = element.Q<Image>();
            if (image != null)
            {
                image.image = mask.texture;
                image.tintColor = new Color(1f, 1f, 1f, mask.texture != null ? Mathf.Clamp01(maskAlpha + 0.12f) : 0f);
                image.style.display = mask.texture != null ? DisplayStyle.Flex : DisplayStyle.None;
            }
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
                    UpdateRow(GetRow(used), $"{name}{track}", detection.confidence, Accent);
                    used++;
                }
            }

            if (showPoses && result.poses != null)
            {
                for (int i = 0; i < result.poses.Length && used < maxRows; i++)
                {
                    VisionPose pose = result.poses[i];
                    string name = pose.personId >= 0 ? $"Pose T{pose.personId}" : "Pose";
                    UpdateRow(GetRow(used), $"{name} · {pose.VisibleKeypointCount} pts", pose.confidence, PoseColor);
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
                    UpdateRow(GetRow(used), $"{name}{track}", mask.confidence, Warning);
                    used++;
                }
            }

            if (used == 0)
            {
                Label empty = GetRow(0);
                empty.text = "No active results";
                empty.style.color = MutedText;
                empty.style.backgroundColor = PanelSoft;
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

        private bool TryGetVisibleKeypoint(VisionPose pose, int index, out VisionKeypoint keypoint)
        {
            keypoint = default;
            if (pose.keypoints == null || index < 0 || index >= pose.keypoints.Length)
                return false;

            keypoint = pose.keypoints[index];
            return IsVisibleKeypoint(keypoint);
        }

        private bool IsVisibleKeypoint(VisionKeypoint keypoint)
        {
            return keypoint.isVisible && keypoint.confidence >= keypointConfidenceThreshold;
        }

        private VisualElement CreateOverlayLayer(string name)
        {
            var layer = new VisualElement { name = name };
            layer.pickingMode = PickingMode.Ignore;
            layer.style.position = Position.Absolute;
            layer.style.left = 0;
            layer.style.right = 0;
            layer.style.top = 0;
            layer.style.bottom = 0;
            return layer;
        }

        private VisualElement CreateDetectionBox()
        {
            var box = new VisualElement();
            box.pickingMode = PickingMode.Ignore;
            box.style.position = Position.Absolute;
            box.style.borderTopWidth = 2;
            box.style.borderRightWidth = 2;
            box.style.borderBottomWidth = 2;
            box.style.borderLeftWidth = 2;
            box.style.borderTopLeftRadius = 4;
            box.style.borderTopRightRadius = 4;
            box.style.borderBottomLeftRadius = 4;
            box.style.borderBottomRightRadius = 4;
            return box;
        }

        private VisualElement CreateMaskOverlay()
        {
            var mask = new VisualElement();
            mask.pickingMode = PickingMode.Ignore;
            mask.style.position = Position.Absolute;
            mask.style.borderTopLeftRadius = 4;
            mask.style.borderTopRightRadius = 4;
            mask.style.borderBottomLeftRadius = 4;
            mask.style.borderBottomRightRadius = 4;

            var image = new Image { scaleMode = ScaleMode.StretchToFill };
            image.pickingMode = PickingMode.Ignore;
            image.style.position = Position.Absolute;
            image.style.left = 0;
            image.style.right = 0;
            image.style.top = 0;
            image.style.bottom = 0;
            mask.Add(image);

            return mask;
        }

        private VisualElement CreateKeypoint()
        {
            var point = new VisualElement();
            point.pickingMode = PickingMode.Ignore;
            point.style.position = Position.Absolute;
            point.style.width = 8;
            point.style.height = 8;
            point.style.borderTopLeftRadius = 8;
            point.style.borderTopRightRadius = 8;
            point.style.borderBottomLeftRadius = 8;
            point.style.borderBottomRightRadius = 8;
            point.style.borderTopWidth = 1;
            point.style.borderRightWidth = 1;
            point.style.borderBottomWidth = 1;
            point.style.borderLeftWidth = 1;
            SetBorderColor(point, Color.black);
            return point;
        }

        private VisualElement CreateBone()
        {
            var bone = new VisualElement();
            bone.pickingMode = PickingMode.Ignore;
            bone.style.position = Position.Absolute;
            return bone;
        }

        private VisualElement CreatePanel(float width, float minHeight)
        {
            var panel = new VisualElement();
            panel.style.backgroundColor = Panel;
            panel.style.borderTopLeftRadius = 8;
            panel.style.borderTopRightRadius = 8;
            panel.style.borderBottomLeftRadius = 8;
            panel.style.borderBottomRightRadius = 8;
            panel.style.borderTopWidth = 1;
            panel.style.borderRightWidth = 1;
            panel.style.borderBottomWidth = 1;
            panel.style.borderLeftWidth = 1;
            panel.style.borderTopColor = Border;
            panel.style.borderRightColor = Border;
            panel.style.borderBottomColor = Border;
            panel.style.borderLeftColor = Border;
            panel.style.paddingLeft = 14;
            panel.style.paddingRight = 14;
            panel.style.paddingTop = 14;
            panel.style.paddingBottom = 14;
            panel.style.minHeight = minHeight;

            if (width > 0f)
                panel.style.width = width;

            return panel;
        }

        private Label AddStat(VisualElement parent, string name, string value)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.marginBottom = 6;
            parent.Add(row);

            var label = new Label(name);
            label.style.color = MutedText;
            label.style.fontSize = 12;
            row.Add(label);

            var number = new Label(value);
            number.style.color = Text;
            number.style.fontSize = 13;
            number.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(number);
            return number;
        }

        private Button CreateCommandButton(string text, Color color)
        {
            var button = new Button { text = text };
            button.style.height = 34;
            button.style.flexGrow = 1f;
            button.style.borderTopLeftRadius = 6;
            button.style.borderTopRightRadius = 6;
            button.style.borderBottomLeftRadius = 6;
            button.style.borderBottomRightRadius = 6;
            button.style.backgroundColor = new Color(color.r, color.g, color.b, 0.18f);
            button.style.color = Text;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            SetBorderColor(button, color);
            return button;
        }

        private Label CreatePill(string text, Color color)
        {
            var label = new Label(text);
            label.style.height = 24;
            label.style.paddingLeft = 8;
            label.style.paddingRight = 8;
            label.style.paddingTop = 3;
            label.style.paddingBottom = 3;
            label.style.borderTopLeftRadius = 6;
            label.style.borderTopRightRadius = 6;
            label.style.borderBottomLeftRadius = 6;
            label.style.borderBottomRightRadius = 6;
            label.style.fontSize = 12;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = Color.black;
            SetPillColor(label, color);
            return label;
        }

        private Label GetRow(int index)
        {
            while (rowPool.Count <= index)
            {
                var row = new Label();
                row.style.height = 30;
                row.style.marginBottom = 6;
                row.style.paddingLeft = 10;
                row.style.paddingRight = 10;
                row.style.paddingTop = 6;
                row.style.paddingBottom = 4;
                row.style.borderTopLeftRadius = 6;
                row.style.borderTopRightRadius = 6;
                row.style.borderBottomLeftRadius = 6;
                row.style.borderBottomRightRadius = 6;
                row.style.fontSize = 12;
                list.Add(row);
                rowPool.Add(row);
            }

            rowPool[index].style.display = DisplayStyle.Flex;
            return rowPool[index];
        }

        private void UpdateRow(Label row, string label, float confidence, Color color)
        {
            row.text = $"{label}  {(confidence * 100f):F0}%";
            row.style.color = Text;
            row.style.backgroundColor = new Color(color.r, color.g, color.b, 0.14f);
        }

        private static VisualElement GetElement(List<VisualElement> pool, VisualElement parent, Func<VisualElement> factory, int index)
        {
            while (pool.Count <= index)
            {
                VisualElement element = factory();
                parent.Add(element);
                pool.Add(element);
            }

            pool[index].style.display = DisplayStyle.Flex;
            return pool[index];
        }

        private static Label GetLabel(List<Label> pool, VisualElement parent, int index)
        {
            while (pool.Count <= index)
            {
                var label = new Label();
                label.pickingMode = PickingMode.Ignore;
                label.style.position = Position.Absolute;
                label.style.paddingLeft = 7;
                label.style.paddingRight = 7;
                label.style.paddingTop = 3;
                label.style.paddingBottom = 3;
                label.style.borderTopLeftRadius = 5;
                label.style.borderTopRightRadius = 5;
                label.style.borderBottomLeftRadius = 5;
                label.style.borderBottomRightRadius = 5;
                label.style.fontSize = 11;
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                parent.Add(label);
                pool.Add(label);
            }

            pool[index].style.display = DisplayStyle.Flex;
            return pool[index];
        }

        private static void SetPoolActive(List<VisualElement> pool, int activeCount)
        {
            for (int i = 0; i < pool.Count; i++)
                pool[i].style.display = i < activeCount ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static void SetLabelsActive(List<Label> pool, int activeCount)
        {
            for (int i = 0; i < pool.Count; i++)
                pool[i].style.display = i < activeCount ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetRowsActive(int activeCount)
        {
            for (int i = 0; i < rowPool.Count; i++)
                rowPool[i].style.display = i < activeCount ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static void SetBorderColor(VisualElement element, Color color)
        {
            element.style.borderTopColor = color;
            element.style.borderRightColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;
        }

        private static void SetPillColor(Label label, Color color)
        {
            label.style.backgroundColor = color;
        }
    }
}
