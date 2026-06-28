using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UniversalTracker.Core;

namespace UniversalTracker.OutputReceivers
{
    internal readonly struct VisionDashboardResultListSettings
    {
        public readonly bool showDetections;
        public readonly bool showPoses;
        public readonly bool showMasks;
        public readonly int maxRows;

        public VisionDashboardResultListSettings(bool showDetections, bool showPoses, bool showMasks, int maxRows)
        {
            this.showDetections = showDetections;
            this.showPoses = showPoses;
            this.showMasks = showMasks;
            this.maxRows = maxRows;
        }
    }

    internal sealed class VisionToolkitDashboardResultListBinder
    {
        private readonly List<Label> rowPool = new List<Label>();
        private VisualElement list;

        public void Bind(VisualElement list)
        {
            this.list = list;
        }

        public void Clear()
        {
            SetRowsActive(0);
        }

        public void UpdateRows(VisionFrameResult result, VisionDashboardResultListSettings settings)
        {
            int used = 0;
            used = AddDetectionRows(result, settings, used);
            used = AddPoseRows(result, settings, used);
            used = AddMaskRows(result, settings, used);

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

        private int AddDetectionRows(VisionFrameResult result, VisionDashboardResultListSettings settings, int used)
        {
            if (!settings.showDetections || result.detections == null)
                return used;

            for (int i = 0; i < result.detections.Length && used < settings.maxRows; i++)
            {
                VisionDetection detection = result.detections[i];
                string name = string.IsNullOrWhiteSpace(detection.label) ? $"Class {detection.classId}" : detection.label;
                string track = detection.IsTracked ? $" T{detection.trackId}" : string.Empty;
                UpdateRow(GetRow(used), $"{name}{track}", detection.confidence, VisionDashboardTheme.Accent);
                used++;
            }

            return used;
        }

        private int AddPoseRows(VisionFrameResult result, VisionDashboardResultListSettings settings, int used)
        {
            if (!settings.showPoses || result.poses == null)
                return used;

            for (int i = 0; i < result.poses.Length && used < settings.maxRows; i++)
            {
                VisionPose pose = result.poses[i];
                string name = pose.personId >= 0 ? $"Pose T{pose.personId}" : "Pose";
                UpdateRow(GetRow(used), $"{name} - {pose.VisibleKeypointCount} pts", pose.confidence, VisionDashboardTheme.PoseColor);
                used++;
            }

            return used;
        }

        private int AddMaskRows(VisionFrameResult result, VisionDashboardResultListSettings settings, int used)
        {
            if (!settings.showMasks || result.masks == null)
                return used;

            for (int i = 0; i < result.masks.Length && used < settings.maxRows; i++)
            {
                VisionMask mask = result.masks[i];
                string name = string.IsNullOrWhiteSpace(mask.label) ? $"Mask {mask.classId}" : mask.label;
                string track = mask.trackId >= 0 ? $" T{mask.trackId}" : string.Empty;
                UpdateRow(GetRow(used), $"{name}{track}", mask.confidence, VisionDashboardTheme.Warning);
                used++;
            }

            return used;
        }

        private Label GetRow(int index)
        {
            while (rowPool.Count <= index)
            {
                Label row = VisionToolkitDashboardPrimitives.CreateResultRow(list);
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
