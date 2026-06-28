using UnityEditor;
using ProAnimaVision.Samples;

namespace ProAnimaVision.Samples.Editor
{
    [CustomEditor(typeof(ProAnimaVisionExperimentalSceneBootstrap))]
    internal sealed class ProAnimaVisionExperimentalSceneBootstrapEditor : UnityEditor.Editor
    {
        private static bool showPreviewOptions;
        private static bool showPipeline;
        private static bool showFallback;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox("Demo scene bootstrap. For the usual flow, choose a preset in ProAnima Vision Control Center and press Play.", MessageType.Info);
            DrawCamera();
            DrawPipeline();
            DrawFallback();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCamera()
        {
            EditorGUILayout.LabelField("Camera", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("runWebCamPreview"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("deviceIndex"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("deviceNameOverride"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("requestedWidth"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("requestedHeight"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("requestedFps"));

            showPreviewOptions = EditorGUILayout.Foldout(showPreviewOptions, "Preview options", true);
            if (showPreviewOptions)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("previewScaleMode"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("previewRotationDegrees"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("mirrorPreviewX"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("mirrorPreviewY"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(6f);
        }

        private void DrawPipeline()
        {
            showPipeline = EditorGUILayout.Foldout(showPipeline, "Real pipeline", true);
            if (!showPipeline)
                return;

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("configureRealPipeline"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("pipelineProfile"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("modelProfile"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoStartRealPipeline"));
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(6f);
        }

        private void DrawFallback()
        {
            showFallback = EditorGUILayout.Foldout(showFallback, "Fallback", true);
            if (!showFallback)
                return;

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("syntheticFallbackWhenNoCamera"));
            EditorGUI.indentLevel--;
        }
    }
}
