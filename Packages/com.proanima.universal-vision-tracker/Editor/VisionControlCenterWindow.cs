using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniversalTracker.Editor
{
    public sealed class VisionControlCenterWindow : EditorWindow
    {
        private const string ExperimentalScenePath =
            "Packages/com.proanima.universal-vision-tracker/Samples~/Experimental Scene/ProAnimaVisionExperimentalScene.unity";
        private const string GettingStartedPath =
            "Packages/com.proanima.universal-vision-tracker/Documentation~/GETTING_STARTED.md";
        private const string RoadmapPath =
            "Packages/com.proanima.universal-vision-tracker/Documentation~/ARCHITECTURE_ROADMAP.md";
        private const string SamplesPath =
            "Packages/com.proanima.universal-vision-tracker/Samples~";

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

            root.Add(CreateHeader());
            root.Add(CreateGrid());
            root.Add(CreateFooter());
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

            var subtitle = new Label("Create profiles, configure scenes, validate compatibility, and open samples from one place.");
            subtitle.style.fontSize = 12;
            subtitle.style.color = new Color(0.62f, 0.72f, 0.76f);
            subtitle.style.marginTop = 4;
            header.Add(subtitle);
            return header;
        }

        private static VisualElement CreateGrid()
        {
            var grid = new VisualElement();
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            grid.style.flexGrow = 1f;

            grid.Add(CreateCard(
                "Start",
                "Build or open the fastest working dashboard scene.",
                ("Open Control Scene", OpenExperimentalScene),
                ("Open Setup Wizard", VisionSetupWizardWindow.Open),
                ("Open Samples Folder", () => EditorUtility.RevealInFinder(SamplesPath))));

            grid.Add(CreateCard(
                "Profiles",
                "Create model and pipeline profiles for runtime use.",
                ("Model Profile Wizard", VisionProfileWizardWindow.Open),
                ("YOLO Detection Profile", VisionProfileAssetCreator.CreateYoloDetectionProfile),
                ("Pipeline Profile", VisionProfileAssetCreator.CreatePipelineProfile)));

            grid.Add(CreateCard(
                "Validate",
                "Check profile readiness before entering Play Mode.",
                ("Compatibility Inspector", VisionProfileCompatibilityWindow.Open),
                ("Profile Validator", VisionProfileValidatorWindow.Open),
                ("Refresh Project", AssetDatabase.Refresh)));

            grid.Add(CreateCard(
                "Docs",
                "Open the short setup guide and architecture notes.",
                ("Getting Started", () => OpenAsset(GettingStartedPath)),
                ("Architecture Roadmap", () => OpenAsset(RoadmapPath)),
                ("Package Manifest", () => OpenAsset("Packages/com.proanima.universal-vision-tracker/package.json"))));

            return grid;
        }

        private static VisualElement CreateFooter()
        {
            var footer = new Label("Recommended flow: Experimental Scene -> Model Profile -> Pipeline Profile -> Setup Wizard -> Compatibility Inspector.");
            footer.style.marginTop = 12;
            footer.style.color = new Color(0.54f, 0.64f, 0.68f);
            footer.style.fontSize = 11;
            return footer;
        }

        private static VisualElement CreateCard(string title, string description, params (string label, System.Action action)[] actions)
        {
            var card = new VisualElement();
            card.style.width = Length.Percent(50);
            card.style.minWidth = 300;
            card.style.paddingRight = 8;
            card.style.paddingBottom = 8;

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
            card.Add(panel);

            var heading = new Label(title);
            heading.style.fontSize = 15;
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.color = new Color(0.92f, 0.96f, 0.98f);
            panel.Add(heading);

            var text = new Label(description);
            text.style.marginTop = 4;
            text.style.marginBottom = 10;
            text.style.whiteSpace = WhiteSpace.Normal;
            text.style.color = new Color(0.62f, 0.72f, 0.76f);
            panel.Add(text);

            for (int i = 0; i < actions.Length; i++)
                panel.Add(CreateButton(actions[i].label, actions[i].action, i == 0));

            return card;
        }

        private static Button CreateButton(string text, System.Action action, bool primary)
        {
            var button = new Button(() => action?.Invoke()) { text = text };
            button.style.height = 30;
            button.style.marginTop = 5;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.color = primary ? Color.black : new Color(0.88f, 0.94f, 0.96f);
            button.style.backgroundColor = primary
                ? new Color(0.42f, 0.86f, 0.72f, 1f)
                : new Color(0.12f, 0.16f, 0.18f, 1f);
            return button;
        }

        private static void OpenExperimentalScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(ExperimentalScenePath);
        }

        private static void OpenAsset(string path)
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (asset != null)
            {
                AssetDatabase.OpenAsset(asset);
                return;
            }

            Debug.LogWarning($"[ProAnima Vision] Asset not found: {path}");
        }
    }
}
