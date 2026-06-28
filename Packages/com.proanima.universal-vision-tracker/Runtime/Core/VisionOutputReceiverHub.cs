using System;
using System.Collections.Generic;
using UnityEngine;
using UniversalTracker.OutputReceivers;

namespace UniversalTracker.Core
{
    internal readonly struct VisionOutputReceiverSettings
    {
        public readonly EventOutputReceiver manualEventReceiver;
        public readonly UIVisualizationReceiver manualUIReceiver;
        public readonly VisionToolkitDashboardReceiver manualToolkitDashboardReceiver;
        public readonly SceneVisualizationReceiver manualSceneReceiver;
        public readonly DebugOutputReceiver manualDebugReceiver;
        public readonly bool useEventOutput;
        public readonly bool useUIVisualization;
        public readonly bool useToolkitDashboard;
        public readonly bool useSceneVisualization;
        public readonly bool useDebugOutput;

        public VisionOutputReceiverSettings(
            EventOutputReceiver manualEventReceiver,
            UIVisualizationReceiver manualUIReceiver,
            VisionToolkitDashboardReceiver manualToolkitDashboardReceiver,
            SceneVisualizationReceiver manualSceneReceiver,
            DebugOutputReceiver manualDebugReceiver,
            bool useEventOutput,
            bool useUIVisualization,
            bool useToolkitDashboard,
            bool useSceneVisualization,
            bool useDebugOutput)
        {
            this.manualEventReceiver = manualEventReceiver;
            this.manualUIReceiver = manualUIReceiver;
            this.manualToolkitDashboardReceiver = manualToolkitDashboardReceiver;
            this.manualSceneReceiver = manualSceneReceiver;
            this.manualDebugReceiver = manualDebugReceiver;
            this.useEventOutput = useEventOutput;
            this.useUIVisualization = useUIVisualization;
            this.useToolkitDashboard = useToolkitDashboard;
            this.useSceneVisualization = useSceneVisualization;
            this.useDebugOutput = useDebugOutput;
        }
    }

    internal sealed class VisionOutputReceiverHub
    {
        private readonly List<IOutputReceiver> receivers = new List<IOutputReceiver>();

        public void Initialize(GameObject host, UniversalTrackerManager manager, VisionOutputReceiverSettings settings)
        {
            Release();

            AddReceiver(settings.manualEventReceiver, settings.useEventOutput, () => host.AddComponent<EventOutputReceiver>());
            AddReceiver(settings.manualUIReceiver, settings.useUIVisualization, () => host.AddComponent<UIVisualizationReceiver>());
            AddReceiver(settings.manualToolkitDashboardReceiver, settings.useToolkitDashboard, () => CreateToolkitDashboardReceiver(host, manager));
            AddReceiver(settings.manualSceneReceiver, settings.useSceneVisualization, () => host.AddComponent<SceneVisualizationReceiver>());
            AddReceiver(settings.manualDebugReceiver, settings.useDebugOutput, () => host.AddComponent<DebugOutputReceiver>());
        }

        public void Dispatch(VisionFrameResult result)
        {
            for (int i = 0; i < receivers.Count; i++)
            {
                IOutputReceiver receiver = receivers[i];
                if (receiver == null || !receiver.IsEnabled)
                    continue;

                try
                {
                    receiver.ReceiveVisionResult(result);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[VisionOutputReceiverHub] Output receiver failed: {e.Message}");
                }
            }
        }

        public void Release()
        {
            for (int i = 0; i < receivers.Count; i++)
            {
                try { receivers[i]?.Release(); } catch { }
            }

            receivers.Clear();
        }

        private static VisionToolkitDashboardReceiver CreateToolkitDashboardReceiver(GameObject host, UniversalTrackerManager manager)
        {
            var receiver = host.AddComponent<VisionToolkitDashboardReceiver>();
            receiver.trackerManager = manager;
            return receiver;
        }

        private void AddReceiver<T>(T manualReceiver, bool createIfMissing, Func<T> factory)
            where T : MonoBehaviour, IOutputReceiver
        {
            T receiver = manualReceiver;
            if (receiver == null && createIfMissing)
                receiver = factory();

            if (receiver == null)
                return;

            receiver.Initialize();
            receivers.Add(receiver);
        }
    }
}
