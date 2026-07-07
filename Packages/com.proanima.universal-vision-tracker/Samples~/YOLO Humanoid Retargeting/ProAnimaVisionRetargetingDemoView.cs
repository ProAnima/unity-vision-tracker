using System;
using System.Collections.Generic;
using UniversalTracker.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniversalTracker.Samples
{
    internal sealed class ProAnimaVisionRetargetingDemoView
    {
        private static readonly Color Background = new Color(0.035f, 0.043f, 0.052f, 0.98f);
        private static readonly Color Panel = new Color(0.06f, 0.08f, 0.1f, 0.95f);
        private static readonly Color Border = new Color(0.26f, 0.34f, 0.4f, 0.74f);
        private static readonly Color Text = new Color(0.9f, 0.96f, 1f, 1f);
        private static readonly Color Muted = new Color(0.58f, 0.68f, 0.75f, 1f);
        private static readonly Color Accent = new Color(0.38f, 0.95f, 0.76f, 1f);
        private readonly MonoBehaviour owner;
        private readonly ProAnimaVisionRetargetingSourceController sources;
        private UIDocument document;
        private PanelSettings panelSettings;
        private Image previewImage;
        private ProAnimaVisionRetargetingPreviewOverlay overlay;
        private DropdownField sourceDropdown;
        private DropdownField cameraDropdown;
        private DropdownField videoDropdown;
        private VisualElement cameraControls;
        private VisualElement videoControls;
        private Label statusLabel;
        private Label metricsLabel;
        private Button previousVideoButton;
        private Button nextVideoButton;

        public ProAnimaVisionRetargetingDemoView(MonoBehaviour owner, ProAnimaVisionRetargetingSourceController sources)
        {
            this.owner = owner;
            this.sources = sources;
        }

        public void Initialize()
        {
            EnsureDocument();
            BuildLayout();
            RefreshControls();
        }

        public void Dispose()
        {
            if (panelSettings != null)
                UnityEngine.Object.Destroy(panelSettings);
        }

        public void Update(VisionFrameResult result)
        {
            EnsureDocument();
            if (previewImage != null)
                previewImage.image = sources.CurrentTexture;

            overlay?.SetResult(result);
            UpdateLabels(result);
        }

        private void EnsureDocument()
        {
            document = owner.GetComponent<UIDocument>() ?? owner.gameObject.AddComponent<UIDocument>();
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                panelSettings.name = "Retargeting Demo Panel";
            }

            if (document.panelSettings == null)
                document.panelSettings = panelSettings;
        }

        private void BuildLayout()
        {
            VisualElement root = document.rootVisualElement;
            root.Clear();
            root.style.flexGrow = 1f;
            root.style.flexDirection = FlexDirection.Row;
            root.style.backgroundColor = Color.clear;

            VisualElement left = CreateLeftPanel();
            VisualElement right = CreateRightOverlay();
            root.Add(left);
            root.Add(right);
        }

        private VisualElement CreateLeftPanel()
        {
            var left = new VisualElement { name = "RetargetingSourcePanel" };
            left.style.width = Length.Percent(50);
            left.style.minWidth = 360;
            left.style.flexShrink = 0f;
            left.style.paddingLeft = 18;
            left.style.paddingRight = 18;
            left.style.paddingTop = 18;
            left.style.paddingBottom = 18;
            left.style.backgroundColor = Background;

            var header = new VisualElement();
            header.style.marginBottom = 12;
            left.Add(header);

            var title = new Label("Retargeting Live Source");
            title.style.fontSize = 21;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Text;
            header.Add(title);

            statusLabel = CreateStatusLabel("Initializing");
            statusLabel.style.marginTop = 8;
            header.Add(statusLabel);

            left.Add(CreateSourceControls());
            left.Add(CreatePreviewStage());

            metricsLabel = new Label();
            metricsLabel.style.color = Muted;
            metricsLabel.style.fontSize = 12;
            metricsLabel.style.marginTop = 10;
            metricsLabel.style.whiteSpace = WhiteSpace.Normal;
            left.Add(metricsLabel);
            return left;
        }

        private VisualElement CreateSourceControls()
        {
            var controls = CreateSection();
            sourceDropdown = CreateDropdown("Source");
            sourceDropdown.choices = new List<string> { "Camera", "Video", "Synthetic" };
            sourceDropdown.RegisterValueChangedCallback(evt => SelectSource(evt.newValue));
            controls.Add(sourceDropdown);

            cameraControls = CreateInlineControls();
            cameraDropdown = CreateDropdown("Camera");
            cameraDropdown.RegisterValueChangedCallback(evt => SelectCamera(evt.newValue));
            cameraControls.Add(cameraDropdown);
            cameraControls.Add(CreateButton("Refresh", RefreshCameraChoices));
            cameraControls.Add(CreateButton("Next", UseNextCamera));
            controls.Add(cameraControls);

            videoControls = CreateInlineControls();
            videoDropdown = CreateDropdown("Video");
            videoDropdown.RegisterValueChangedCallback(evt => SelectVideo(evt.newValue));
            videoControls.Add(videoDropdown);
            previousVideoButton = CreateButton("<", UsePreviousVideo);
            nextVideoButton = CreateButton(">", UseNextVideo);
            previousVideoButton.tooltip = "Previous video";
            nextVideoButton.tooltip = "Next video";
            videoControls.Add(previousVideoButton);
            videoControls.Add(nextVideoButton);
            controls.Add(videoControls);
            return controls;
        }

        private VisualElement CreatePreviewStage()
        {
            var stage = new VisualElement { name = "RetargetingSourcePreview" };
            stage.style.flexGrow = 1f;
            stage.style.minHeight = 320;
            stage.style.backgroundColor = new Color(0.02f, 0.026f, 0.032f, 1f);
            stage.style.borderTopLeftRadius = 8;
            stage.style.borderTopRightRadius = 8;
            stage.style.borderBottomLeftRadius = 8;
            stage.style.borderBottomRightRadius = 8;
            stage.style.borderTopWidth = 1;
            stage.style.borderRightWidth = 1;
            stage.style.borderBottomWidth = 1;
            stage.style.borderLeftWidth = 1;
            SetBorder(stage, Border);

            previewImage = new Image { name = "RetargetingSourceImage", scaleMode = ScaleMode.ScaleToFit };
            previewImage.style.position = Position.Absolute;
            previewImage.style.left = 0;
            previewImage.style.right = 0;
            previewImage.style.top = 0;
            previewImage.style.bottom = 0;
            stage.Add(previewImage);

            overlay = new ProAnimaVisionRetargetingPreviewOverlay();
            stage.Add(overlay);
            return stage;
        }

        private static VisualElement CreateRightOverlay()
        {
            var right = new VisualElement { name = "RetargetedRigPanel" };
            right.pickingMode = PickingMode.Ignore;
            right.style.flexGrow = 1f;
            right.style.paddingLeft = 18;
            right.style.paddingRight = 18;
            right.style.paddingTop = 18;

            var label = CreateStatusLabel("RETARGETED UNITY RIG");
            label.style.alignSelf = Align.FlexStart;
            right.Add(label);
            return right;
        }

        private void SelectSource(string value)
        {
            if (!Enum.TryParse(value, out ProAnimaVisionRetargetingSourceMode mode))
                return;

            sources.SetMode(mode);
            RefreshControls();
        }

        private void RefreshCameraChoices()
        {
            sources.RefreshCameraChoices();
            RefreshControls();
            if (sources.Mode == ProAnimaVisionRetargetingSourceMode.Camera)
                sources.SetMode(sources.Mode);
        }

        private void SelectCamera(string value)
        {
            sources.SelectCamera(value);
            RefreshControls();
        }

        private void UseNextCamera()
        {
            sources.UseNextCamera();
            RefreshControls();
        }

        private void SelectVideo(string value)
        {
            int index = videoDropdown?.choices?.IndexOf(value) ?? -1;
            if (index >= 0)
                sources.SelectVideo(index);

            RefreshControls();
        }

        private void UsePreviousVideo()
        {
            sources.UsePreviousVideo();
            RefreshControls();
        }

        private void UseNextVideo()
        {
            sources.UseNextVideo();
            RefreshControls();
        }

        private void RefreshControls()
        {
            if (sourceDropdown == null)
                return;

            RefreshControlValues();
            cameraControls.style.display = sources.Mode == ProAnimaVisionRetargetingSourceMode.Camera ? DisplayStyle.Flex : DisplayStyle.None;
            videoControls.style.display = sources.Mode == ProAnimaVisionRetargetingSourceMode.Video ? DisplayStyle.Flex : DisplayStyle.None;
            bool canSwitchVideo = sources.HasSwitchableVideo();
            previousVideoButton?.SetEnabled(canSwitchVideo);
            nextVideoButton?.SetEnabled(canSwitchVideo);
        }

        private void RefreshControlValues()
        {
            sourceDropdown.SetValueWithoutNotify(sources.Mode.ToString());

            cameraDropdown.choices = new List<string>(sources.CameraChoices);
            cameraDropdown.SetValueWithoutNotify(sources.ResolveCameraChoice());

            sources.RefreshVideoChoices();
            videoDropdown.choices = new List<string>(sources.VideoChoices);
            int videoIndex = Mathf.Clamp(sources.CurrentVideoChoiceIndex(), 0, Mathf.Max(0, videoDropdown.choices.Count - 1));
            if (videoDropdown.choices.Count > 0)
                videoDropdown.SetValueWithoutNotify(videoDropdown.choices[videoIndex]);
        }

        private void UpdateLabels(VisionFrameResult result)
        {
            if (statusLabel != null)
                statusLabel.text = sources.Status;

            if (metricsLabel == null)
                return;

            int poseCount = result?.poses?.Length ?? 0;
            int visible = poseCount > 0 ? result.poses[0].VisibleKeypointCount : 0;
            int detections = result?.detections?.Length ?? 0;
            Vector2Int size = result?.sourceSize ?? Vector2Int.zero;
            metricsLabel.text = $"pose {poseCount} | visible {visible}/17 | detections {detections} | source {size.x}x{size.y}";
        }

        private static VisualElement CreateSection()
        {
            var section = new VisualElement();
            section.style.backgroundColor = Panel;
            section.style.borderTopLeftRadius = 8;
            section.style.borderTopRightRadius = 8;
            section.style.borderBottomLeftRadius = 8;
            section.style.borderBottomRightRadius = 8;
            section.style.borderTopWidth = 1;
            section.style.borderRightWidth = 1;
            section.style.borderBottomWidth = 1;
            section.style.borderLeftWidth = 1;
            section.style.paddingLeft = 12;
            section.style.paddingRight = 12;
            section.style.paddingTop = 12;
            section.style.paddingBottom = 6;
            section.style.marginBottom = 12;
            SetBorder(section, Border);
            return section;
        }

        private static VisualElement CreateInlineControls()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;
            row.style.alignItems = Align.FlexEnd;
            row.style.marginTop = 8;
            return row;
        }

        private static DropdownField CreateDropdown(string label)
        {
            var dropdown = new DropdownField(label);
            dropdown.style.flexGrow = 1f;
            dropdown.style.minWidth = 180;
            dropdown.style.marginRight = 8;
            dropdown.style.marginBottom = 6;
            dropdown.style.color = Text;
            return dropdown;
        }

        private static Button CreateButton(string text, Action action)
        {
            var button = new Button(() => action?.Invoke()) { text = text };
            button.style.height = 28;
            button.style.minWidth = 68;
            button.style.marginRight = 6;
            button.style.marginBottom = 6;
            button.style.borderTopLeftRadius = 6;
            button.style.borderTopRightRadius = 6;
            button.style.borderBottomLeftRadius = 6;
            button.style.borderBottomRightRadius = 6;
            button.style.backgroundColor = new Color(0.12f, 0.16f, 0.18f, 1f);
            button.style.color = Text;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            return button;
        }

        private static Label CreateStatusLabel(string text)
        {
            var label = new Label(text);
            label.style.paddingLeft = 8;
            label.style.paddingRight = 8;
            label.style.paddingTop = 4;
            label.style.paddingBottom = 4;
            label.style.borderTopLeftRadius = 6;
            label.style.borderTopRightRadius = 6;
            label.style.borderBottomLeftRadius = 6;
            label.style.borderBottomRightRadius = 6;
            label.style.backgroundColor = new Color(Accent.r, Accent.g, Accent.b, 0.16f);
            label.style.color = Text;
            label.style.fontSize = 12;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            return label;
        }

        private static void SetBorder(VisualElement element, Color color)
        {
            element.style.borderTopColor = color;
            element.style.borderRightColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;
        }
    }
}
