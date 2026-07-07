using UnityEngine;
using UnityEngine.UIElements;

namespace UniversalTracker.OutputReceivers
{
    internal static class VisionToolkitDashboardPrimitives
    {
        public const string MaskImageName = "VisionMaskTexture";
        public const string LineCoreName = "VisionLineCore";
        public const string KeypointCoreName = "VisionKeypointCore";

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
            box.style.overflow = Overflow.Visible;
            box.style.borderTopWidth = 2;
            box.style.borderRightWidth = 2;
            box.style.borderBottomWidth = 2;
            box.style.borderLeftWidth = 2;
            box.style.borderTopLeftRadius = 4;
            box.style.borderTopRightRadius = 4;
            box.style.borderBottomLeftRadius = 4;
            box.style.borderBottomRightRadius = 4;

            for (int i = 0; i < 8; i++)
                box.Add(CreateDetectionCorner(i));

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

            var image = new Image { name = MaskImageName, scaleMode = ScaleMode.StretchToFill };
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

            var core = new VisualElement { name = KeypointCoreName };
            core.pickingMode = PickingMode.Ignore;
            core.style.position = Position.Absolute;
            core.style.left = 2;
            core.style.right = 2;
            core.style.top = 2;
            core.style.bottom = 2;
            core.style.borderTopLeftRadius = 8;
            core.style.borderTopRightRadius = 8;
            core.style.borderBottomLeftRadius = 8;
            core.style.borderBottomRightRadius = 8;
            point.Add(core);
            return point;
        }

        public static VisualElement CreateBone()
        {
            return CreateLineSegment();
        }

        public static VisualElement CreateContourSegment()
        {
            return CreateLineSegment();
        }

        public static VisualElement CreateLineCore()
        {
            var core = new VisualElement { name = LineCoreName };
            core.pickingMode = PickingMode.Ignore;
            core.style.position = Position.Absolute;
            return core;
        }

        private static VisualElement CreateLineSegment()
        {
            var bone = new VisualElement();
            bone.pickingMode = PickingMode.Ignore;
            bone.style.position = Position.Absolute;
            bone.style.overflow = Overflow.Visible;
            bone.Add(CreateLineCore());
            return bone;
        }

        private static VisualElement CreateDetectionCorner(int index)
        {
            var corner = new VisualElement { name = $"VisionDetectionCorner{index}" };
            corner.pickingMode = PickingMode.Ignore;
            corner.style.position = Position.Absolute;
            corner.style.borderTopLeftRadius = 2;
            corner.style.borderTopRightRadius = 2;
            corner.style.borderBottomLeftRadius = 2;
            corner.style.borderBottomRightRadius = 2;

            switch (index)
            {
                case 0:
                case 1:
                    corner.style.left = 0;
                    corner.style.top = 0;
                    break;
                case 2:
                case 3:
                    corner.style.right = 0;
                    corner.style.top = 0;
                    break;
                case 4:
                case 5:
                    corner.style.left = 0;
                    corner.style.bottom = 0;
                    break;
                default:
                    corner.style.right = 0;
                    corner.style.bottom = 0;
                    break;
            }

            return corner;
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
            label.style.borderTopWidth = 1;
            label.style.borderRightWidth = 1;
            label.style.borderBottomWidth = 1;
            label.style.borderLeftWidth = 1;
            label.style.fontSize = 11;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.whiteSpace = WhiteSpace.NoWrap;
            VisionDashboardTheme.SetBorderColor(label, new Color(0f, 0f, 0f, 0.4f));
            return label;
        }
    }
}
