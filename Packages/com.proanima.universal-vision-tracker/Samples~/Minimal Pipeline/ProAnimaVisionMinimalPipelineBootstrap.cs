using UnityEngine;
using UniversalTracker;
using UniversalTracker.Core;
using UniversalTracker.OutputReceivers;

namespace ProAnimaVision.Samples
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UniversalTrackerManager))]
    public sealed class ProAnimaVisionMinimalPipelineBootstrap : MonoBehaviour
    {
        public VisionPipelineProfile pipelineProfile;
        public VisionModelProfile modelProfile;
        public InputProviderType frameSource = InputProviderType.WebCam;
        public bool autoStart = true;
        public bool enableTracking = true;
        public bool addDashboard = true;
        [Range(1, 120)] public int targetFps = 30;

        private void Reset()
        {
            Apply();
        }

        [ContextMenu("Apply Sample Setup")]
        public void Apply()
        {
            UniversalTrackerManager manager = GetComponent<UniversalTrackerManager>();
            manager.pipelineProfile = pipelineProfile;
            manager.modelProfiles = pipelineProfile == null && modelProfile != null
                ? new[] { modelProfile }
                : null;
            manager.inputType = frameSource;
            manager.autoStart = autoStart;
            manager.useTracking = enableTracking;
            manager.targetFPS = Mathf.Clamp(targetFps, 1, 120);
            manager.useEventOutput = true;
            manager.useDebugOutput = false;
            manager.useUIVisualization = false;
            manager.useSceneVisualization = false;
            manager.useToolkitDashboard = addDashboard;

            if (addDashboard)
                ConfigureDashboard(manager);
        }

        private void ConfigureDashboard(UniversalTrackerManager manager)
        {
            VisionToolkitDashboardReceiver dashboard = GetComponent<VisionToolkitDashboardReceiver>();
            if (dashboard == null)
                dashboard = gameObject.AddComponent<VisionToolkitDashboardReceiver>();

            dashboard.trackerManager = manager;
            dashboard.autoFindManager = false;
            dashboard.subscribeToManagerEvent = true;
            manager.manualToolkitDashboardReceiver = dashboard;
        }
    }
}
