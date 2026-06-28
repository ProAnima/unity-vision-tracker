using UnityEngine;
using UnityEngine.UIElements;
using UniversalTracker.Core;

namespace UniversalTracker.OutputReceivers
{
    internal static class VisionDashboardTheme
    {
        public static readonly Color Panel = new Color(0.06f, 0.08f, 0.1f, 0.92f);
        public static readonly Color PanelSoft = new Color(0.09f, 0.12f, 0.15f, 0.92f);
        public static readonly Color Border = new Color(0.26f, 0.34f, 0.4f, 0.7f);
        public static readonly Color Text = new Color(0.9f, 0.96f, 1f, 1f);
        public static readonly Color MutedText = new Color(0.58f, 0.68f, 0.75f, 1f);
        public static readonly Color Accent = new Color(0.16f, 0.78f, 0.98f, 1f);
        public static readonly Color Good = new Color(0.38f, 0.95f, 0.55f, 1f);
        public static readonly Color Warning = new Color(1f, 0.72f, 0.28f, 1f);
        public static readonly Color PoseColor = new Color(0.88f, 0.96f, 0.45f, 1f);

        public static Color HealthColor(VisionHealthState state)
        {
            return state switch
            {
                VisionHealthState.Running => Good,
                VisionHealthState.Degraded => Warning,
                VisionHealthState.Recovering => Warning,
                VisionHealthState.Failed => new Color(1f, 0.35f, 0.35f, 1f),
                VisionHealthState.Initializing => Accent,
                _ => Warning
            };
        }

        public static void SetBorderColor(VisualElement element, Color color)
        {
            element.style.borderTopColor = color;
            element.style.borderRightColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;
        }

        public static void SetPillColor(Label label, Color color)
        {
            label.style.backgroundColor = color;
        }
    }
}
