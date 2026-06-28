using UnityEngine;
using UnityEngine.UIElements;

namespace UniversalTracker.OutputReceivers
{
    internal static class VisionToolkitDashboardPrimitives
    {
        public static VisualElement CreateOverlayLayer(string name)
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

        public static VisualElement CreateDetectionBox()
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

        public static VisualElement CreateMaskOverlay()
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

        public static VisualElement CreateKeypoint()
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
            VisionDashboardTheme.SetBorderColor(point, Color.black);
            return point;
        }

        public static VisualElement CreateBone()
        {
            var bone = new VisualElement();
            bone.pickingMode = PickingMode.Ignore;
            bone.style.position = Position.Absolute;
            return bone;
        }

        public static Label CreateResultRow(VisualElement list)
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
            return row;
        }

        public static Label CreateOverlayLabel()
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
            return label;
        }
    }
}
