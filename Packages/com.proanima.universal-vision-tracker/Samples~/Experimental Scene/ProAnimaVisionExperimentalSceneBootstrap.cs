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
        [Header("Demo Mode")]
        public bool runSyntheticPreview = true;
        [Range(1, 60)] public int syntheticFps = 24;

        [Header("Real Pipeline Hookup")]
        public bool configureRealPipeline;
        public VisionPipelineProfile pipelineProfile;
        public VisionModelProfile modelProfile;
        public InputProviderType frameSource = InputProviderType.WebCam;
        public bool autoStartRealPipeline;

        [Header("Dashboard")]
        public bool showDetections = true;
        public bool showPoses = true;
        public bool showMasks = true;

        private UIDocument document;
        private PanelSettings panelSettings;
        private VisionToolkitDashboardReceiver dashboard;
        private UniversalTrackerManager manager;
        private Texture2D previewTexture;
        private float nextFrameTime;
        private int frameIndex;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            EnsureCamera();
            EnsureDashboard();
            EnsureManager();
        }

        private void Start()
        {
            if (configureRealPipeline && autoStartRealPipeline && manager != null)
                manager.StartTracking();
        }

        private void Update()
        {
            if (!runSyntheticPreview || dashboard == null || Time.time < nextFrameTime)
                return;

            nextFrameTime = Time.time + 1f / Mathf.Max(1, syntheticFps);
            UpdatePreviewTexture();
            dashboard.ReceiveVisionResult(CreateSyntheticResult(), previewTexture);
        }

        private void OnDestroy()
        {
            if (previewTexture != null)
                Destroy(previewTexture);

            if (panelSettings != null)
                Destroy(panelSettings);
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
            panelSettings.name = "ProAnima Vision Experimental Panel";
            document.panelSettings = panelSettings;

            dashboard = GetComponent<VisionToolkitDashboardReceiver>() ?? gameObject.AddComponent<VisionToolkitDashboardReceiver>();
            dashboard.autoFindManager = false;
            dashboard.subscribeToManagerEvent = configureRealPipeline;
            dashboard.showDetections = showDetections;
            dashboard.showPoses = showPoses;
            dashboard.showMasks = showMasks;
            dashboard.showStats = true;
            dashboard.showOverlayMetrics = true;
            dashboard.maxRows = 10;

            previewTexture = new Texture2D(320, 180, TextureFormat.RGBA32, false);
            previewTexture.name = "ProAnima Vision Synthetic Preview";
        }

        private void EnsureManager()
        {
            manager = GetComponent<UniversalTrackerManager>() ?? gameObject.AddComponent<UniversalTrackerManager>();
            manager.pipelineProfile = pipelineProfile;
            manager.modelProfiles = pipelineProfile == null && modelProfile != null ? new[] { modelProfile } : null;
            manager.inputType = frameSource;
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

        private void UpdatePreviewTexture()
        {
            float pulse = Mathf.Sin(Time.time * 1.6f) * 0.5f + 0.5f;
            Color left = Color.Lerp(new Color(0.035f, 0.06f, 0.075f), new Color(0.05f, 0.1f, 0.11f), pulse);
            Color right = Color.Lerp(new Color(0.12f, 0.13f, 0.17f), new Color(0.18f, 0.12f, 0.16f), pulse);

            for (int y = 0; y < previewTexture.height; y++)
            {
                for (int x = 0; x < previewTexture.width; x++)
                {
                    float horizontal = (float)x / Mathf.Max(1, previewTexture.width - 1);
                    float grid = x % 32 == 0 || y % 32 == 0 ? 0.055f : 0f;
                    previewTexture.SetPixel(x, y, Color.Lerp(left, right, horizontal) + new Color(grid, grid, grid, 0f));
                }
            }

            previewTexture.Apply(false);
        }
        private VisionFrameResult CreateSyntheticResult()
        {
            float walk = Mathf.PingPong(Time.time * 0.12f, 0.28f);
            var person = new Rect(0.24f + walk, 0.16f, 0.26f, 0.66f);
            var tool = new Rect(0.61f - walk * 0.32f, 0.52f, 0.17f, 0.18f);

            return new VisionFrameResult
            {
                frameIndex = frameIndex++,
                timestamp = Time.timeAsDouble,
                sourceSize = new Vector2Int(1280, 720),
                detections = new[]
                {
                    Detection(101, 0, "performer", 0.94f, person),
                    Detection(212, 39, "tracked prop", 0.86f, tool)
                },
                poses = new[] { Pose(101, person) },
                masks = new[] { Mask(101, "silhouette", 0.89f, person) },
                stats = VisionPerformanceStats.FromStages(0.7f, 6.4f, 1.1f, 0.35f)
            };
        }
        private static VisionDetection Detection(int trackId, int classId, string label, float confidence, Rect rect)
        {
            return new VisionDetection
            {
                trackId = trackId,
                classId = classId,
                label = label,
                confidence = confidence,
                normalizedRect = rect,
                sourceRect = Scale(rect),
                sourceCenter = new Vector2(rect.center.x * 1280f, rect.center.y * 720f),
                trackState = VisionTrackState.Tracking
            };
        }
        private static VisionMask Mask(int trackId, string label, float confidence, Rect rect)
        {
            return new VisionMask
            {
                trackId = trackId,
                classId = 0,
                label = label,
                confidence = confidence,
                normalizedRect = rect,
                sourceRect = Scale(rect)
            };
        }
        private static VisionPose Pose(int personId, Rect rect)
        {
            return new VisionPose
            {
                personId = personId,
                confidence = 0.92f,
                normalizedRect = rect,
                sourceRect = Scale(rect),
                keypoints = CreateKeypoints(rect),
                skeleton = new VisionSkeletonDefinition
                {
                    name = "experimental-coco",
                    bones = new[]
                    {
                        Bone(5, 6, "shoulders"),
                        Bone(5, 11, "left torso"),
                        Bone(6, 12, "right torso"),
                        Bone(11, 12, "hips"),
                        Bone(5, 7, "left arm"),
                        Bone(6, 8, "right arm")
                    }
                },
                trackState = VisionTrackState.Tracking
            };
        }
        private static VisionKeypoint[] CreateKeypoints(Rect rect)
        {
            var keypoints = new VisionKeypoint[17];
            keypoints[5] = Keypoint(5, "left shoulder", rect, 0.34f, 0.24f);
            keypoints[6] = Keypoint(6, "right shoulder", rect, 0.66f, 0.24f);
            keypoints[7] = Keypoint(7, "left elbow", rect, 0.24f, 0.42f);
            keypoints[8] = Keypoint(8, "right elbow", rect, 0.76f, 0.42f);
            keypoints[11] = Keypoint(11, "left hip", rect, 0.42f, 0.68f);
            keypoints[12] = Keypoint(12, "right hip", rect, 0.58f, 0.68f);
            return keypoints;
        }

        private static VisionKeypoint Keypoint(int index, string name, Rect rect, float x, float y)
        {
            Vector2 normalized = new Vector2(rect.x + rect.width * x, rect.y + rect.height * y);
            return new VisionKeypoint
            {
                index = index,
                name = name,
                normalizedPosition = normalized,
                sourcePosition = new Vector2(normalized.x * 1280f, normalized.y * 720f),
                confidence = 0.91f,
                isVisible = true
            };
        }

        private static VisionSkeletonBone Bone(int from, int to, string name)
        {
            return new VisionSkeletonBone { from = from, to = to, name = name };
        }

        private static Rect Scale(Rect rect)
        {
            return new Rect(rect.x * 1280f, rect.y * 720f, rect.width * 1280f, rect.height * 720f);
        }
    }
}
