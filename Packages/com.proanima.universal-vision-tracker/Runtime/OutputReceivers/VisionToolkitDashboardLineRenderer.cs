using UnityEngine;
using UnityEngine.UIElements;
using UniversalTracker.Visualization;

namespace UniversalTracker.OutputReceivers
{
    internal static class VisionToolkitDashboardLineRenderer
    {
        public static void UpdateLine(
            VisualElement lineElement,
            Vector2 from,
            Vector2 to,
            float coreThickness,
            float haloThickness,
            Color color,
            float haloAlpha)
        {
            BoneLine line = VisionDashboardGeometry.CalculateBoneLine(from, to);
            float height = Mathf.Max(coreThickness, haloThickness);
            lineElement.style.left = line.center.x - line.length * 0.5f;
            lineElement.style.top = line.center.y - height * 0.5f;
            lineElement.style.width = Mathf.Max(1f, line.length);
            lineElement.style.height = Mathf.Max(1f, height);
            lineElement.style.rotate = new Rotate(new Angle(line.angleDegrees, AngleUnit.Degree));
            lineElement.style.backgroundColor = VisionDashboardOverlayStyle.WithAlpha(Color.black, haloAlpha);
            SetRadius(lineElement, height);

            VisualElement core = EnsureCore(lineElement);
            core.style.left = 0;
            core.style.right = 0;
            core.style.top = Mathf.Max(0f, (height - coreThickness) * 0.5f);
            core.style.height = Mathf.Max(1f, coreThickness);
            core.style.backgroundColor = color;
            SetRadius(core, coreThickness);
        }

        private static VisualElement EnsureCore(VisualElement lineElement)
        {
            if (lineElement.childCount > 0)
                return lineElement.ElementAt(0);

            VisualElement core = VisionToolkitDashboardPrimitives.CreateLineCore();
            lineElement.Add(core);
            return core;
        }

        private static void SetRadius(VisualElement element, float radius)
        {
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
        }
    }
}
