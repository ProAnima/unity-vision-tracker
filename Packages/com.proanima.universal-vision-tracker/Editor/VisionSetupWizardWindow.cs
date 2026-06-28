using UnityEditor;
using UnityEngine;
using UniversalTracker.Core;

namespace UniversalTracker.Editor
{
    public sealed class VisionSetupWizardWindow : EditorWindow
    {
        private string objectName = VisionSceneSetupUtility.DefaultObjectName;
        private VisionPipelineProfile pipelineProfile;
        private VisionModelProfile modelProfile;
        private VisionSceneSetupSource source = VisionSceneSetupSource.WebCam;
        private bool addDashboard = true;
        private bool enableTracking = true;
        private bool autoStart = true;
        private int targetFps = 30;
        private Vector2 scroll;

        [MenuItem("Tools/ProAnima Vision/Advanced/Setup Wizard", priority = 0)]
        public static void Open()
        {
            var window = GetWindow<VisionSetupWizardWindow>("Vision Setup");
            window.minSize = new Vector2(460f, 430f);
            window.Show();
        }

        private void OnGUI()
        {
            DrawHeader();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawProfiles();
            DrawSceneOptions();
            DrawActions();
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("ProAnima Vision Setup Wizard", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Create or update a profile-driven tracker object in the current scene. Use this for quick setup, samples, and production scene bootstrapping.",
                MessageType.Info);
        }

        private void DrawProfiles()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Profiles", EditorStyles.boldLabel);
                pipelineProfile = (VisionPipelineProfile)EditorGUILayout.ObjectField("Pipeline Profile", pipelineProfile, typeof(VisionPipelineProfile), false);
                using (new EditorGUI.DisabledScope(pipelineProfile != null))
                    modelProfile = (VisionModelProfile)EditorGUILayout.ObjectField("Model Profile", modelProfile, typeof(VisionModelProfile), false);

                if (pipelineProfile != null)
                    EditorGUILayout.HelpBox("Pipeline Profile will be used as the primary configuration source.", MessageType.Info);
                else if (modelProfile != null)
                    EditorGUILayout.HelpBox("A single-model setup will be created for this scene. You can convert it to a Pipeline Profile later.", MessageType.Info);
            }
        }

        private void DrawSceneOptions()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Scene Object", EditorStyles.boldLabel);
                objectName = EditorGUILayout.TextField("Object Name", objectName);
                source = (VisionSceneSetupSource)EditorGUILayout.EnumPopup("Frame Source", source);
                targetFps = EditorGUILayout.IntSlider("Target FPS", targetFps, 1, 120);
                autoStart = EditorGUILayout.Toggle("Auto Start", autoStart);
                enableTracking = EditorGUILayout.Toggle("Tracking", enableTracking);
                addDashboard = EditorGUILayout.Toggle("UI Toolkit Dashboard", addDashboard);
            }
        }

        private void DrawActions()
        {
            EditorGUILayout.Space(8f);
            bool canCreate = VisionSceneSetupUtility.CanCreate(pipelineProfile, modelProfile, out string reason);
            if (!canCreate)
                EditorGUILayout.HelpBox(reason, MessageType.Warning);

            using (new EditorGUI.DisabledScope(!canCreate))
            {
                if (GUILayout.Button("Create Or Update Scene Tracker", GUILayout.Height(34f)))
                    CreateOrUpdate();
            }
        }

        private void CreateOrUpdate()
        {
            var options = new VisionSceneSetupOptions(
                objectName,
                pipelineProfile,
                modelProfile,
                source,
                addDashboard,
                enableTracking,
                autoStart,
                targetFps);

            VisionSceneSetupUtility.CreateOrUpdate(options);
        }
    }
}
