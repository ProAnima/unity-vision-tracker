using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniversalTracker.Editor
{
    public sealed class VisionControlCenterWindow : EditorWindow
    {
        private const string GettingStartedPath =
            "Packages/com.proanima.universal-vision-tracker/Documentation~/GETTING_STARTED.md";
        private const string RoadmapPath =
            "Packages/com.proanima.universal-vision-tracker/Documentation~/ARCHITECTURE_ROADMAP.md";
        private const string SamplesPath =
            "Packages/com.proanima.universal-vision-tracker/Samples~";
        private VisionQuickStartPresetDefinition selectedPreset = VisionQuickStartPresetDefinition.All[0];
        private Label presetDescriptionLabel;
        private Label presetRequirementLabel;
        private Button presetActionButton;

        [MenuItem("Tools/ProAnima Vision/Control Center", priority = -100)]
        public static void Open()
        {
            var window = GetWindow<VisionControlCenterWindow>("ProAnima Vision");
            window.minSize = new Vector2(720f, 560f);
            window.Show();
        }

        private void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.Clear();
            root.style.backgroundColor = new Color(0.035f, 0.043f, 0.052f, 1f);
            root.style.paddingLeft = 18;
            root.style.paddingRight = 18;
            root.style.paddingTop = 18;
            root.style.paddingBottom = 18;

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1f;
            root.Add(scroll);

            scroll.Add(CreateHeader());
            scroll.Add(CreatePreviewSceneBlock());
            scroll.Add(CreatePresetBlock());
            scroll.Add(CreateAdvancedBlock());
        }

        private static VisualElement CreateHeader()
        {
            var header = new VisualElement();
            header.style.marginBottom = 14;

            var title = new Label("ProAnima Vision Control Center");
            title.style.fontSize = 24;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new Color(0.92f, 0.96f, 0.98f);
            header.Add(title);

            var subtitle = new Label("One place to import the sample, create profiles, configure a scene, and validate runtime readiness.");
            subtitle.style.fontSize = 12;
            subtitle.style.color = new Color(0.62f, 0.72f, 0.76f);
            subtitle.style.marginTop = 4;
            header.Add(subtitle);
            return header;
        }

        private VisualElement CreatePreviewSceneBlock()
        {
            VisualElement panel = CreatePanel();

            var heading = CreateHeading("Scene Preview");
            panel.Add(heading);
            panel.Add(CreateBodyText("Import and open the polished source demo scene. This is the fastest way to verify WebCam access, VideoPlayer wiring, preview fitting, rotation, mirroring, and overlay layout."));

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;
            row.style.marginTop = 8;
            panel.Add(row);

            row.Add(CreateButton("Import / Open Demo Scene", () => EnsureExperimentalSceneImportedAndOpen(), true));
            row.Add(CreateButton("Open Samples Folder", () => EditorUtility.RevealInFinder(SamplesPath), false));
            return panel;
        }

        private VisualElement CreatePresetBlock()
        {
            VisualElement panel = CreatePanel();
            panel.style.backgroundColor = new Color(0.055f, 0.075f, 0.082f, 1f);

            panel.Add(CreateHeading("Quick Start Preset"));
            panel.Add(CreateBodyText("Choose the scenario you want to run. The preset creates or updates the required profiles, opens the demo scene, wires the dashboard, and keeps advanced setup hidden."));

            var dropdown = new DropdownField("Preset", VisionQuickStartPresetDefinition.Labels(), selectedPreset.Label);
            dropdown.style.marginTop = 10;
            dropdown.style.marginBottom = 8;
            dropdown.RegisterValueChangedCallback(evt =>
            {
                selectedPreset = VisionQuickStartPresetDefinition.FromLabel(evt.newValue);
                RefreshPresetDetails();
            });
            panel.Add(dropdown);

            presetDescriptionLabel = CreateBodyText(string.Empty);
            presetDescriptionLabel.style.marginTop = 2;
            panel.Add(presetDescriptionLabel);

            presetRequirementLabel = CreateMutedPill(string.Empty);
            presetRequirementLabel.style.marginTop = 8;
            panel.Add(presetRequirementLabel);

            presetActionButton = CreateButton(string.Empty, () => VisionQuickStartPresetUtility.Apply(selectedPreset.Preset), true);
            presetActionButton.style.marginTop = 12;
            panel.Add(presetActionButton);

            RefreshPresetDetails();
            return panel;
        }

        private static VisualElement CreateAdvancedBlock()
        {
            var foldout = new Foldout { text = "Advanced tools", value = false };
            foldout.style.marginTop = 4;
            foldout.style.color = new Color(0.78f, 0.88f, 0.88f, 1f);

            VisualElement panel = CreatePanel();
            panel.style.marginTop = 8;
            foldout.Add(panel);

            panel.Add(CreateBodyText("Use these only when you need to inspect profiles, validate compatibility, or build a custom scene manually."));

            var grid = new VisualElement();
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            grid.style.marginTop = 8;
            panel.Add(grid);

            grid.Add(CreateButton("Model Profile Wizard", VisionProfileWizardWindow.Open, false));
            grid.Add(CreateButton("Pipeline Profile", VisionProfileAssetCreator.CreatePipelineProfile, false));
            grid.Add(CreateButton("Setup Wizard", VisionSetupWizardWindow.Open, false));
            grid.Add(CreateButton("Compatibility Inspector", VisionProfileCompatibilityWindow.Open, false));
            grid.Add(CreateButton("Profile Validator", VisionProfileValidatorWindow.Open, false));
            grid.Add(CreateButton("Getting Started", () => OpenAsset(GettingStartedPath), false));
            grid.Add(CreateButton("Architecture Roadmap", () => OpenAsset(RoadmapPath), false));
            return foldout;
        }

        private void RefreshPresetDetails()
        {
            if (presetDescriptionLabel != null)
                presetDescriptionLabel.text = selectedPreset.Description;

            if (presetRequirementLabel != null)
                presetRequirementLabel.text = selectedPreset.Requirement;

            if (presetActionButton != null)
                presetActionButton.text = selectedPreset.ActionLabel;
        }

        private static VisualElement CreatePanel()
        {
            var panel = new VisualElement();
            panel.style.backgroundColor = new Color(0.07f, 0.085f, 0.098f, 1f);
            panel.style.borderTopLeftRadius = 8;
            panel.style.borderTopRightRadius = 8;
            panel.style.borderBottomLeftRadius = 8;
            panel.style.borderBottomRightRadius = 8;
            panel.style.borderTopWidth = 1;
            panel.style.borderRightWidth = 1;
            panel.style.borderBottomWidth = 1;
            panel.style.borderLeftWidth = 1;
            panel.style.borderTopColor = new Color(0.16f, 0.22f, 0.24f);
            panel.style.borderRightColor = new Color(0.16f, 0.22f, 0.24f);
            panel.style.borderBottomColor = new Color(0.16f, 0.22f, 0.24f);
            panel.style.borderLeftColor = new Color(0.16f, 0.22f, 0.24f);
            panel.style.paddingLeft = 14;
            panel.style.paddingRight = 14;
            panel.style.paddingTop = 14;
            panel.style.paddingBottom = 14;
            panel.style.marginBottom = 12;
            return panel;
        }

        private static Label CreateHeading(string text)
        {
            var heading = new Label(text);
            heading.style.fontSize = 16;
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.color = new Color(0.92f, 0.96f, 0.98f);
            return heading;
        }

        private static Label CreateBodyText(string text)
        {
            var label = new Label(text);
            label.style.marginTop = 4;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.color = new Color(0.62f, 0.72f, 0.76f);
            return label;
        }

        private static Label CreateMutedPill(string text)
        {
            var label = new Label(text);
            label.style.alignSelf = Align.FlexStart;
            label.style.paddingLeft = 8;
            label.style.paddingRight = 8;
            label.style.paddingTop = 4;
            label.style.paddingBottom = 4;
            label.style.borderTopLeftRadius = 6;
            label.style.borderTopRightRadius = 6;
            label.style.borderBottomLeftRadius = 6;
            label.style.borderBottomRightRadius = 6;
            label.style.backgroundColor = new Color(0.12f, 0.16f, 0.18f, 1f);
            label.style.color = new Color(0.78f, 0.88f, 0.88f, 1f);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            return label;
        }

        private static Button CreateButton(string text, System.Action action, bool primary)
        {
            var button = new Button(() => action?.Invoke()) { text = text };
            button.style.height = 30;
            button.style.minWidth = 150;
            button.style.marginRight = 7;
            button.style.marginTop = 5;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.color = primary ? Color.black : new Color(0.88f, 0.94f, 0.96f);
            button.style.backgroundColor = primary
                ? new Color(0.42f, 0.86f, 0.72f, 1f)
                : new Color(0.12f, 0.16f, 0.18f, 1f);
            return button;
        }

        private static void ImportExperimentalScene()
        {
            ImportAndOpenExperimentalScene();
        }

        internal static void ImportAndOpenExperimentalScene()
        {
            VisionExperimentalSceneSampleUtility.ImportAndOpen();
        }

        internal static bool EnsureExperimentalSceneImportedAndOpen()
        {
            return VisionExperimentalSceneSampleUtility.EnsureImportedAndOpen();
        }

        private static void OpenAsset(string path)
        {
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (asset != null)
            {
                AssetDatabase.OpenAsset(asset);
                return;
            }

            Debug.LogWarning($"[ProAnima Vision] Asset not found: {path}");
        }
    }
}
