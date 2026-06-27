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
        public bool showStats = true;
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
        private VisualElement detectionLayer;
        private VisualElement boneLayer;
        private VisualElement keypointLayer;
        private VisualElement list;
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

        private readonly List<VisualElement> detectionPool = new List<VisualElement>();
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
            SetPoolActive(detectionPool, 0);
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

            boneLayer = CreateOverlayLayer("Bones");
            detectionLayer = CreateOverlayLayer("Detections");
            keypointLayer = CreateOverlayLayer("Keypoints");
            overlay.Add(boneLayer);
            overlay.Add(detectionLayer);
            overlay.Add(keypointLayer);
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

            int detectionsUsed = 0;
            if (showDetections && result.detections != null)
            {
                for (int i = 0; i < result.detections.Length; i++)
                    UpdateDetectionBox(GetElement(detectionPool, detectionLayer, CreateDetectionBox, detectionsUsed), detectionsUsed++, result.detections[i], sourceSize, viewportSize);
            }

            int keypointsUsed = 0;
            int bonesUsed = 0;
            if (showPoses && result.poses != null)
            {
                for (int i = 0; i < result.poses.Length; i++)
                    UpdatePose(result.poses[i], sourceSize, viewportSize, ref keypointsUsed, ref bonesUsed);
            }

            SetPoolActive(detectionPool, detectionsUsed);
            SetPoolActive(keypointPool, keypointsUsed);
            SetPoolActive(bonePool, bonesUsed);
        }

        private void UpdateDetectionBox(VisualElement box, int index, VisionDetection detection, Vector2 sourceSize, Vector2 viewportSize)
        {
            Rect rect = VisionDashboardGeometry.NormalizedToViewportRect(detection.normalizedRect, sourceSize, viewportSize);
            box.style.left = rect.x;
            box.style.top = rect.y;
            box.style.width = Mathf.Max(1f, rect.width);
            box.style.height = Mathf.Max(1f, rect.height);

            var label = box.Q<Label>();
            string name = string.IsNullOrWhiteSpace(detection.label) ? $"#{detection.classId}" : detection.label;
            string id = detection.IsTracked ? $" T{detection.trackId}" : string.Empty;
            label.text = $"{name} {(detection.confidence * 100f):F0}%{id}";

            Color color = index % 2 == 0 ? Accent : Good;
            SetBorderColor(box, color);
            label.style.backgroundColor = new Color(color.r, color.g, color.b, 0.88f);
        }

        private void UpdatePose(VisionPose pose, Vector2 sourceSize, Vector2 viewportSize, ref int keypointsUsed, ref int bonesUsed)
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
                    UpdateBone(GetElement(bonePool, boneLayer, CreateBone, bonesUsed), bonesUsed++, fromPoint, toPoint);
                }
            }

            for (int i = 0; i < pose.keypoints.Length; i++)
            {
                if (!pose.keypoints[i].isVisible)
                    continue;

                Vector2 point = VisionDashboardGeometry.NormalizedToViewportPoint(pose.keypoints[i].normalizedPosition, sourceSize, viewportSize);
                UpdateKeypoint(GetElement(keypointPool, keypointLayer, CreateKeypoint, keypointsUsed), keypointsUsed++, point, pose.keypoints[i]);
            }
        }

        private void UpdateBone(VisualElement bone, int index, Vector2 from, Vector2 to)
        {
            BoneLine line = VisionDashboardGeometry.CalculateBoneLine(from, to);
            bone.style.left = line.center.x - line.length * 0.5f;
            bone.style.top = line.center.y - 1f;
            bone.style.width = Mathf.Max(1f, line.length);
            bone.style.height = 2;
            bone.style.rotate = new Rotate(new Angle(line.angleDegrees, AngleUnit.Degree));
            bone.style.backgroundColor = index % 2 == 0 ? PoseColor : Accent;
        }

        private void UpdateKeypoint(VisualElement keypoint, int index, Vector2 point, VisionKeypoint data)
        {
            keypoint.style.left = point.x - 4f;
            keypoint.style.top = point.y - 4f;
            keypoint.style.backgroundColor = data.confidence >= 0.5f ? PoseColor : Warning;
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

        private static bool TryGetVisibleKeypoint(VisionPose pose, int index, out VisionKeypoint keypoint)
        {
            keypoint = default;
            if (pose.keypoints == null || index < 0 || index >= pose.keypoints.Length)
                return false;

            keypoint = pose.keypoints[index];
            return keypoint.isVisible;
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

            var label = new Label();
            label.style.position = Position.Absolute;
            label.style.left = 0;
            label.style.top = -24;
            label.style.height = 22;
            label.style.paddingLeft = 6;
            label.style.paddingRight = 6;
            label.style.paddingTop = 2;
            label.style.paddingBottom = 2;
            label.style.borderTopLeftRadius = 4;
            label.style.borderTopRightRadius = 4;
            label.style.borderBottomLeftRadius = 4;
            label.style.borderBottomRightRadius = 4;
            label.style.fontSize = 11;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = Color.black;
            box.Add(label);

            return box;
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

        private static void SetPoolActive(List<VisualElement> pool, int activeCount)
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
