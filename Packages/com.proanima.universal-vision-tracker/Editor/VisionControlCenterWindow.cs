using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
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
            scroll.Add(CreateWorkflow());
            scroll.Add(CreateGrid());
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

        private static VisualElement CreateWorkflow()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;
            row.style.marginBottom = 14;
            row.Add(CreateStep("1", "Preview"));
            row.Add(CreateStep("2", "Profiles"));
            row.Add(CreateStep("3", "Setup"));
            row.Add(CreateStep("4", "Validate"));
            return row;
        }

        private static Label CreateStep(string number, string text)
        {
            var step = new Label($"{number}. {text}");
            step.style.marginRight = 8;
            step.style.marginBottom = 6;
            step.style.paddingLeft = 9;
            step.style.paddingRight = 9;
            step.style.paddingTop = 5;
            step.style.paddingBottom = 5;
            step.style.borderTopLeftRadius = 6;
            step.style.borderTopRightRadius = 6;
            step.style.borderBottomLeftRadius = 6;
            step.style.borderBottomRightRadius = 6;
            step.style.backgroundColor = new Color(0.1f, 0.14f, 0.16f, 1f);
            step.style.color = new Color(0.78f, 0.88f, 0.88f, 1f);
            step.style.unityFontStyleAndWeight = FontStyle.Bold;
            return step;
        }

        private static VisualElement CreateGrid()
        {
            var grid = new VisualElement();
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            grid.style.flexGrow = 1f;

            grid.Add(CreateCard(
                "1. Preview",
                "Import the package sample once, then open the polished dashboard scene.",
                ("Import Sample", ImportExperimentalScene),
                ("Open Scene", OpenExperimentalScene),
                ("Open Samples Folder", () => EditorUtility.RevealInFinder(SamplesPath))));

            grid.Add(CreateCard(
                "2. Profiles",
                "Create runtime-ready model and pipeline assets.",
                ("Model Profile Wizard", VisionProfileWizardWindow.Open),
                ("Pipeline Profile", VisionProfileAssetCreator.CreatePipelineProfile),
                ("YOLO Detection Profile", VisionProfileAssetCreator.CreateYoloDetectionProfile)));

            grid.Add(CreateCard(
                "3. Setup",
                "Create scene objects, connect profiles, and inspect compatibility.",
                ("Open Setup Wizard", VisionSetupWizardWindow.Open),
                ("Compatibility Inspector", VisionProfileCompatibilityWindow.Open)));

            grid.Add(CreateCard(
                "4. Validate",
                "Run readiness checks and open the shortest reference docs.",
                ("Profile Validator", VisionProfileValidatorWindow.Open),
                ("Getting Started", () => OpenAsset(GettingStartedPath)),
                ("Architecture Roadmap", () => OpenAsset(RoadmapPath))));

            return grid;
        }

        private static VisualElement CreateCard(string title, string description, params (string label, System.Action action)[] actions)
        {
            var card = new VisualElement();
            card.style.width = Length.Percent(50);
            card.style.minWidth = 300;
            card.style.maxWidth = Length.Percent(100);
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

            string path = FindExperimentalScenePath();
            if (!string.IsNullOrWhiteSpace(path))
            {
                EditorSceneManager.OpenScene(path);
                return;
            }

            EditorUtility.DisplayDialog(
                "Experimental Scene",
                "Import the Experimental Scene sample from Package Manager, then open it from Control Center.",
                "OK");
        }

        private static void ImportExperimentalScene()
        {
            bool packageManagerOpened = TryOpenPackageManager();
            string message = packageManagerOpened
                ? "In Package Manager, select ProAnima Universal Vision Tracker, open Samples, and import Experimental Scene."
                : "Open Window > Package Management > Package Manager, select ProAnima Universal Vision Tracker, open Samples, and import Experimental Scene.";

            EditorUtility.DisplayDialog(
                "Import Experimental Scene",
                message,
                "OK");
        }

        private static bool TryOpenPackageManager()
        {
            Type packageManagerWindow = Type.GetType(
                "UnityEditor.PackageManager.UI.Window,UnityEditor.PackageManagerUI.Editor");
            if (packageManagerWindow == null)
                return false;

            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
            MethodInfo openPackage = packageManagerWindow.GetMethod(
                "Open",
                flags,
                null,
                new[] { typeof(string) },
                null);
            if (TryInvoke(openPackage, "com.proanima.universal-vision-tracker"))
                return true;

            MethodInfo open = packageManagerWindow.GetMethod(
                "Open",
                flags,
                null,
                Type.EmptyTypes,
                null);
            return TryInvoke(open);
        }

        private static bool TryInvoke(MethodInfo method, params object[] args)
        {
            if (method == null)
                return false;

            try
            {
                method.Invoke(null, args);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[ProAnima Vision] Package Manager could not be opened: {exception.Message}");
                return false;
            }
        }

        private static string FindExperimentalScenePath()
        {
            string[] guids = AssetDatabase.FindAssets("ProAnimaVisionExperimentalScene t:Scene", new[] { "Assets" });
            if (guids != null && guids.Length > 0)
                return AssetDatabase.GUIDToAssetPath(guids[0]);

            return null;
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
