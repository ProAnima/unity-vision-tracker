using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Video;
using UniversalTracker.Core;
using UniversalTracker.OutputReceivers;

namespace UniversalTracker.Editor
{
    internal enum VisionSceneSetupSource
    {
        WebCam,
        UnityCamera,
        Texture,
        RenderTexture,
        Video
    }

    internal readonly struct VisionSceneSetupOptions
    {
        public readonly string objectName;
        public readonly VisionPipelineProfile pipelineProfile;
        public readonly VisionModelProfile modelProfile;
        public readonly VisionSceneSetupSource source;
        public readonly bool addDashboard;
        public readonly bool enableTracking;
        public readonly bool autoStart;
        public readonly int targetFps;

        public VisionSceneSetupOptions(
            string objectName,
            VisionPipelineProfile pipelineProfile,
            VisionModelProfile modelProfile,
            VisionSceneSetupSource source,
            bool addDashboard,
            bool enableTracking,
            bool autoStart,
            int targetFps)
        {
            this.objectName = objectName;
            this.pipelineProfile = pipelineProfile;
            this.modelProfile = modelProfile;
            this.source = source;
            this.addDashboard = addDashboard;
            this.enableTracking = enableTracking;
            this.autoStart = autoStart;
            this.targetFps = Mathf.Clamp(targetFps, 1, 120);
        }
    }

    internal readonly struct VisionSceneSetupResult
    {
        public readonly GameObject root;
        public readonly UniversalTrackerManager manager;
        public readonly VisionToolkitDashboardReceiver dashboard;

        public VisionSceneSetupResult(GameObject root, UniversalTrackerManager manager, VisionToolkitDashboardReceiver dashboard)
        {
            this.root = root;
            this.manager = manager;
            this.dashboard = dashboard;
        }
    }

    internal static class VisionSceneSetupUtility
    {
        public const string DefaultObjectName = "ProAnima Vision Tracker";

        public static VisionSceneSetupResult CreateOrUpdate(VisionSceneSetupOptions options)
        {
            GameObject root = ResolveRoot(options.objectName);
            Undo.RegisterFullObjectHierarchyUndo(root, "Configure ProAnima Vision Tracker");

            UniversalTrackerManager manager = root.GetComponent<UniversalTrackerManager>();
            if (manager == null)
                manager = Undo.AddComponent<UniversalTrackerManager>(root);

            ApplyManagerSettings(manager, options);
            VisionToolkitDashboardReceiver dashboard = options.addDashboard
                ? EnsureDashboard(root, manager)
                : null;

            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(manager);
            if (dashboard != null)
                EditorUtility.SetDirty(dashboard);

            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
            return new VisionSceneSetupResult(root, manager, dashboard);
        }

        public static bool CanCreate(VisionPipelineProfile pipelineProfile, VisionModelProfile modelProfile, out string reason)
        {
            if (pipelineProfile == null && modelProfile == null)
            {
                reason = "Assign a Pipeline Profile or a Model Profile.";
                return false;
            }

            reason = null;
            return true;
        }

        private static GameObject ResolveRoot(string objectName)
        {
            string safeName = string.IsNullOrWhiteSpace(objectName) ? DefaultObjectName : objectName.Trim();
            GameObject selected = Selection.activeGameObject;
            if (selected != null && selected.GetComponent<UniversalTrackerManager>() != null)
                return selected;

            GameObject existing = GameObject.Find(safeName);
            if (existing != null)
                return existing;

            var root = new GameObject(safeName);
            Undo.RegisterCreatedObjectUndo(root, "Create ProAnima Vision Tracker");
            return root;
        }

        private static void ApplyManagerSettings(UniversalTrackerManager manager, VisionSceneSetupOptions options)
        {
            manager.autoStart = options.autoStart;
            manager.targetFPS = options.targetFps;
            manager.useTracking = options.enableTracking;
            manager.pipelineProfile = options.pipelineProfile;
            manager.modelProfiles = options.pipelineProfile == null && options.modelProfile != null
                ? new[] { options.modelProfile }
                : null;
            manager.activeModelIndex = 0;
            manager.inputType = ToInputProviderType(options.source);
            manager.sourceVideoPlayer = options.source == VisionSceneSetupSource.Video ? EnsureVideoPlayer(manager.gameObject) : null;
            manager.useToolkitDashboard = options.addDashboard;
            manager.useDebugOutput = false;
            manager.useUIVisualization = false;
            manager.useSceneVisualization = false;
            manager.useEventOutput = true;
        }

        private static VisionToolkitDashboardReceiver EnsureDashboard(GameObject root, UniversalTrackerManager manager)
        {
            VisionToolkitDashboardReceiver dashboard = root.GetComponent<VisionToolkitDashboardReceiver>();
            if (dashboard == null)
                dashboard = Undo.AddComponent<VisionToolkitDashboardReceiver>(root);

            dashboard.autoFindManager = false;
            dashboard.subscribeToManagerEvent = true;
            manager.manualToolkitDashboardReceiver = dashboard;
            return dashboard;
        }

        private static VideoPlayer EnsureVideoPlayer(GameObject root)
        {
            VideoPlayer player = root.GetComponent<VideoPlayer>();
            if (player == null)
                player = Undo.AddComponent<VideoPlayer>(root);

            player.playOnAwake = false;
            player.waitForFirstFrame = true;
            player.isLooping = true;
            player.renderMode = VideoRenderMode.APIOnly;
            EditorUtility.SetDirty(player);
            return player;
        }

        private static InputProviderType ToInputProviderType(VisionSceneSetupSource source)
        {
            return source switch
            {
                VisionSceneSetupSource.UnityCamera => InputProviderType.Camera,
                VisionSceneSetupSource.Texture => InputProviderType.Texture,
                VisionSceneSetupSource.RenderTexture => InputProviderType.RenderTexture,
                VisionSceneSetupSource.Video => InputProviderType.Video,
                _ => InputProviderType.WebCam
            };
        }
    }
}
