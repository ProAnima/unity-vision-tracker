using UnityEditor;
using UnityEngine;
using UniversalTracker;
using UniversalTracker.Core;

namespace UniversalTracker.Editor
{
    [CustomEditor(typeof(UniversalTrackerManager))]
    internal sealed class UniversalTrackerManagerEditor : UnityEditor.Editor
    {
        private static bool showAdvanced;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox("Runtime facade for the active VisionPipeline. Most projects only need Runtime, Source, Model, and Dashboard.", MessageType.Info);
            DrawRuntime();
            DrawSource();
            DrawModel();
            DrawDashboard();
            DrawAdvanced();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawRuntime()
        {
            EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoStart"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetFPS"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useTracking"));
            EditorGUILayout.Space(6f);
        }

        private void DrawSource()
        {
            EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
            SerializedProperty inputType = serializedObject.FindProperty("inputType");
            EditorGUILayout.PropertyField(inputType);

            switch ((InputProviderType)inputType.enumValueIndex)
            {
                case InputProviderType.WebCam:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("webCamDeviceName"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("webCamRequestedWidth"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("webCamRequestedHeight"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("webCamRequestedFps"));
                    break;
                case InputProviderType.Texture:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("sourceTexture"));
                    break;
                case InputProviderType.RenderTexture:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("sourceRenderTexture"));
                    break;
                case InputProviderType.Camera:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("sourceCamera"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraTargetTexture"));
                    break;
                case InputProviderType.Video:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("sourceVideoPlayer"));
                    break;
                case InputProviderType.Custom:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("customInputProvider"));
                    break;
            }

            EditorGUILayout.Space(6f);
        }

        private void DrawModel()
        {
            EditorGUILayout.LabelField("Model", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("pipelineProfile"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("activeModelIndex"));
            EditorGUILayout.Space(6f);
        }

        private void DrawDashboard()
        {
            EditorGUILayout.LabelField("Dashboard", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useToolkitDashboard"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("manualToolkitDashboardReceiver"));
            EditorGUILayout.Space(6f);
        }

        private void DrawAdvanced()
        {
            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced options", true);
            if (!showAdvanced)
                return;

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("modelProfiles"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("manualUIReceiver"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("manualEventReceiver"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("manualSceneReceiver"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("manualDebugReceiver"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useEventOutput"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useUIVisualization"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useSceneVisualization"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useDebugOutput"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("trackerType"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("trackingIoUThreshold"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxMissedFrames"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxConsecutiveErrors"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("verboseLogging"));
            EditorGUI.indentLevel--;
        }
    }
}
