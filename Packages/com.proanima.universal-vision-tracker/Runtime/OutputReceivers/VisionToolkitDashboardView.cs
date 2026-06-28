using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniversalTracker.OutputReceivers
{
    internal sealed class VisionToolkitDashboardView
    {
        public VisualElement root;
        public VisualElement previewStage;
        public Image previewImage;
        public VisualElement overlay;
        public VisualElement contentGuide;
        public VisualElement maskLayer;
        public VisualElement detectionLayer;
        public VisualElement boneLayer;
        public VisualElement keypointLayer;
        public VisualElement labelLayer;
        public VisualElement list;
        public Label overlayMetricsLabel;
        public Label statusLabel;
        public Label frameLabel;
        public Label fpsLabel;
        public Label inferenceLabel;
        public Label budgetLabel;
        public Label sourceLabel;
        public Label modelLabel;
        public Label runtimeLabel;
        public Label diagnosticsLabel;
        public Label detectionCountLabel;
        public Label poseCountLabel;
        public Label errorLabel;
        public Label lastErrorLabel;
        public Button startButton;
        public Button stopButton;
    }

    internal static class VisionToolkitDashboardViewBuilder
    {
        public static VisionToolkitDashboardView Build(UIDocument document, bool isEnabled, Action start, Action stop)
        {
            if (document == null || document.rootVisualElement == null)
                return null;

            var view = new VisionToolkitDashboardView();
            view.root = document.rootVisualElement;
            ConfigureRoot(view.root, isEnabled);

            var side = CreatePanel(300, 0);
            side.name = "VisionControlPanel";
            side.style.marginRight = 14;
            side.style.marginBottom = 14;
            side.style.flexShrink = 0f;
            side.style.minWidth = 260;
            side.style.maxWidth = Length.Percent(100);
            view.root.Add(side);

            AddHeader(side, view);
            AddControls(side, view, start, stop);
            AddStats(side, view);
            AddResultList(side, view);
            AddPreview(view);
            return view;
        }

        private static void ConfigureRoot(VisualElement root, bool isEnabled)
        {
            root.Clear();
            root.style.flexGrow = 1f;
            root.style.flexDirection = FlexDirection.Row;
            root.style.flexWrap = Wrap.Wrap;
            root.style.paddingLeft = 18;
            root.style.paddingRight = 18;
            root.style.paddingTop = 18;
            root.style.paddingBottom = 18;
            root.style.backgroundColor = new Color(0.015f, 0.02f, 0.024f, 0.94f);
            root.style.display = isEnabled ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static void AddPreview(VisionToolkitDashboardView view)
        {
            view.previewStage = new VisualElement { name = "VisionPreviewStage" };
            view.previewStage.style.flexGrow = 1f;
            view.previewStage.style.flexBasis = 520;
            view.previewStage.style.minWidth = 320;
            view.previewStage.style.minHeight = 260;
            view.previewStage.style.backgroundColor = new Color(0.02f, 0.026f, 0.032f, 1f);
            view.previewStage.style.borderTopLeftRadius = 8;
            view.previewStage.style.borderTopRightRadius = 8;
            view.previewStage.style.borderBottomLeftRadius = 8;
            view.previewStage.style.borderBottomRightRadius = 8;
            view.previewStage.style.borderTopWidth = 1;
            view.previewStage.style.borderRightWidth = 1;
            view.previewStage.style.borderBottomWidth = 1;
            view.previewStage.style.borderLeftWidth = 1;
            VisionDashboardTheme.SetBorderColor(view.previewStage, VisionDashboardTheme.Border);
            view.root.Add(view.previewStage);

            view.previewImage = new Image { name = "VisionPreviewImage", scaleMode = ScaleMode.ScaleToFit };
            view.previewImage.style.position = Position.Absolute;
            view.previewImage.style.left = 0;
            view.previewImage.style.right = 0;
            view.previewImage.style.top = 0;
            view.previewImage.style.bottom = 0;
            view.previewStage.Add(view.previewImage);

            view.overlay = new VisualElement { name = "VisionOverlay" };
            view.overlay.pickingMode = PickingMode.Ignore;
            view.overlay.style.position = Position.Absolute;
            view.overlay.style.left = 0;
            view.overlay.style.right = 0;
            view.overlay.style.top = 0;
            view.overlay.style.bottom = 0;
            view.previewStage.Add(view.overlay);

            AddOverlayLayers(view);
            AddOverlayMetrics(view);
        }

        private static void AddOverlayLayers(VisionToolkitDashboardView view)
        {
            view.contentGuide = VisionToolkitDashboardPrimitives.CreateOverlayLayer("VisionContentGuide");
            view.contentGuide.style.borderTopWidth = 1;
            view.contentGuide.style.borderRightWidth = 1;
            view.contentGuide.style.borderBottomWidth = 1;
            view.contentGuide.style.borderLeftWidth = 1;
            VisionDashboardTheme.SetBorderColor(view.contentGuide, new Color(1f, 1f, 1f, 0.18f));
            view.overlay.Add(view.contentGuide);

            view.maskLayer = VisionToolkitDashboardPrimitives.CreateOverlayLayer("Masks");
            view.boneLayer = VisionToolkitDashboardPrimitives.CreateOverlayLayer("Bones");
            view.detectionLayer = VisionToolkitDashboardPrimitives.CreateOverlayLayer("Detections");
            view.keypointLayer = VisionToolkitDashboardPrimitives.CreateOverlayLayer("Keypoints");
            view.labelLayer = VisionToolkitDashboardPrimitives.CreateOverlayLayer("Labels");
            view.overlay.Add(view.maskLayer);
            view.overlay.Add(view.boneLayer);
            view.overlay.Add(view.detectionLayer);
            view.overlay.Add(view.keypointLayer);
            view.overlay.Add(view.labelLayer);
        }

        private static void AddOverlayMetrics(VisionToolkitDashboardView view)
        {
            view.overlayMetricsLabel = new Label();
            view.overlayMetricsLabel.pickingMode = PickingMode.Ignore;
            view.overlayMetricsLabel.style.position = Position.Absolute;
            view.overlayMetricsLabel.style.right = 10;
            view.overlayMetricsLabel.style.top = 10;
            view.overlayMetricsLabel.style.paddingLeft = 8;
            view.overlayMetricsLabel.style.paddingRight = 8;
            view.overlayMetricsLabel.style.paddingTop = 5;
            view.overlayMetricsLabel.style.paddingBottom = 5;
            view.overlayMetricsLabel.style.borderTopLeftRadius = 6;
            view.overlayMetricsLabel.style.borderTopRightRadius = 6;
            view.overlayMetricsLabel.style.borderBottomLeftRadius = 6;
            view.overlayMetricsLabel.style.borderBottomRightRadius = 6;
            view.overlayMetricsLabel.style.backgroundColor = new Color(0f, 0f, 0f, 0.52f);
            view.overlayMetricsLabel.style.color = VisionDashboardTheme.Text;
            view.overlayMetricsLabel.style.fontSize = 11;
            view.overlay.Add(view.overlayMetricsLabel);
        }

        private static void AddHeader(VisualElement parent, VisionToolkitDashboardView view)
        {
            var title = new Label("ProAnima Vision");
            title.style.fontSize = 24;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = VisionDashboardTheme.Text;
            title.style.marginBottom = 4;
            parent.Add(title);

            view.statusLabel = CreatePill("Standing by", VisionDashboardTheme.Warning);
            view.statusLabel.style.alignSelf = Align.FlexStart;
            view.statusLabel.style.marginBottom = 14;
            parent.Add(view.statusLabel);
        }

        private static void AddControls(VisualElement parent, VisionToolkitDashboardView view, Action start, Action stop)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 14;
            row.style.flexWrap = Wrap.Wrap;
            parent.Add(row);

            view.startButton = CreateCommandButton("Start", VisionDashboardTheme.Good);
            view.startButton.clicked += () => start?.Invoke();
            view.startButton.style.marginBottom = 6;
            row.Add(view.startButton);

            view.stopButton = CreateCommandButton("Stop", VisionDashboardTheme.Warning);
            view.stopButton.clicked += () => stop?.Invoke();
            view.stopButton.style.marginLeft = 8;
            view.stopButton.style.marginBottom = 6;
            row.Add(view.stopButton);
        }

        private static void AddStats(VisualElement parent, VisionToolkitDashboardView view)
        {
            var stats = CreatePanel(0, 0);
            stats.style.marginBottom = 14;
            parent.Add(stats);

            view.frameLabel = AddStat(stats, "Frame", "-");
            view.fpsLabel = AddStat(stats, "Runtime FPS", "-");
            view.inferenceLabel = AddStat(stats, "Inference", "-");
            view.budgetLabel = AddStat(stats, "Budget", "-");
            view.sourceLabel = AddStat(stats, "Source", "-");
            view.modelLabel = AddStat(stats, "Model", "-");
            view.runtimeLabel = AddStat(stats, "Runtime", "-");
            view.diagnosticsLabel = AddStat(stats, "Model Output", "-", true);
            view.detectionCountLabel = AddStat(stats, "Detections", "0");
            view.poseCountLabel = AddStat(stats, "Poses", "0");
            view.errorLabel = AddStat(stats, "Errors", "0");
            view.lastErrorLabel = AddStat(stats, "Last Error", "-", true);
        }

        private static void AddResultList(VisualElement parent, VisionToolkitDashboardView view)
        {
            var caption = new Label("Live Results");
            caption.style.fontSize = 12;
            caption.style.color = VisionDashboardTheme.MutedText;
            caption.style.marginBottom = 6;
            parent.Add(caption);

            view.list = new VisualElement();
            view.list.style.flexGrow = 1f;
            parent.Add(view.list);
        }

        private static VisualElement CreatePanel(float width, float minHeight)
        {
            var panel = new VisualElement();
            panel.style.backgroundColor = VisionDashboardTheme.Panel;
            panel.style.borderTopLeftRadius = 8;
            panel.style.borderTopRightRadius = 8;
            panel.style.borderBottomLeftRadius = 8;
            panel.style.borderBottomRightRadius = 8;
            panel.style.borderTopWidth = 1;
            panel.style.borderRightWidth = 1;
            panel.style.borderBottomWidth = 1;
            panel.style.borderLeftWidth = 1;
            VisionDashboardTheme.SetBorderColor(panel, VisionDashboardTheme.Border);
            panel.style.paddingLeft = 14;
            panel.style.paddingRight = 14;
            panel.style.paddingTop = 14;
            panel.style.paddingBottom = 14;
            panel.style.minHeight = minHeight;

            if (width > 0f)
                panel.style.width = width;

            return panel;
        }

        private static Label AddStat(VisualElement parent, string name, string value, bool multiline = false)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.alignItems = multiline ? Align.FlexStart : Align.Center;
            row.style.marginBottom = 6;
            parent.Add(row);

            var label = new Label(name);
            label.style.color = VisionDashboardTheme.MutedText;
            label.style.fontSize = 12;
            label.style.minWidth = multiline ? 76 : 88;
            label.style.flexShrink = 0f;
            row.Add(label);

            var number = new Label(value);
            number.style.color = VisionDashboardTheme.Text;
            number.style.fontSize = 13;
            number.style.unityFontStyleAndWeight = FontStyle.Bold;
            number.style.flexGrow = 1f;
            number.style.flexShrink = 1f;
            number.style.unityTextAlign = TextAnchor.UpperRight;
            number.style.whiteSpace = multiline ? WhiteSpace.Normal : WhiteSpace.NoWrap;
            row.Add(number);
            return number;
        }

        private static Button CreateCommandButton(string text, Color color)
        {
            var button = new Button { text = text };
            button.style.height = 34;
            button.style.flexGrow = 1f;
            button.style.minWidth = 108;
            button.style.borderTopLeftRadius = 6;
            button.style.borderTopRightRadius = 6;
            button.style.borderBottomLeftRadius = 6;
            button.style.borderBottomRightRadius = 6;
            button.style.backgroundColor = new Color(color.r, color.g, color.b, 0.18f);
            button.style.color = VisionDashboardTheme.Text;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            VisionDashboardTheme.SetBorderColor(button, color);
            return button;
        }

        private static Label CreatePill(string text, Color color)
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
            VisionDashboardTheme.SetPillColor(label, color);
            return label;
        }
    }
}
