using UnityEditor;
using UnityEngine;
using UniversalTracker.Core;

namespace UniversalTracker.Editor
{
    public sealed class VisionProfileValidatorWindow : EditorWindow
    {
        private VisionModelProfile modelProfile;
        private VisionPipelineProfile pipelineProfile;
        private bool requireRuntimeAssets = true;
        private Vector2 scroll;
        private VisionProfileValidationReport modelReport;
        private VisionProfileValidationReport pipelineReport;

        [MenuItem("Tools/ProAnima Vision/Advanced/Profile Validator")]
        public static void Open()
        {
            var window = GetWindow<VisionProfileValidatorWindow>("Vision Profiles");
            window.minSize = new Vector2(480f, 420f);
            window.PullFromSelection();
            window.Show();
        }

        [MenuItem("Assets/ProAnima Vision/Validate Selected Profiles", true)]
        private static bool CanValidateSelectedProfiles()
        {
            return Selection.activeObject is VisionModelProfile || Selection.activeObject is VisionPipelineProfile;
        }

        [MenuItem("Assets/ProAnima Vision/Validate Selected Profiles")]
        private static void ValidateSelectedProfiles()
        {
            Open();
        }

        private void OnSelectionChange()
        {
            if (focusedWindow == this)
            {
                PullFromSelection();
                Repaint();
            }
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawTargets();
            DrawActions();

            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawReport("Model Profile", modelReport);
            DrawReport("Pipeline Profile", pipelineReport);
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            VisionEditorGui.DrawHeader(
                "ProAnima Vision Profile Validator",
                "Validate model and pipeline profile contracts before runtime startup.");
        }

        private void DrawTargets()
        {
            using (VisionEditorGui.Section("Targets"))
            {
                modelProfile = (VisionModelProfile)EditorGUILayout.ObjectField("Model Profile", modelProfile, typeof(VisionModelProfile), false);
                pipelineProfile = (VisionPipelineProfile)EditorGUILayout.ObjectField("Pipeline Profile", pipelineProfile, typeof(VisionPipelineProfile), false);
                requireRuntimeAssets = EditorGUILayout.Toggle("Require Runtime Assets", requireRuntimeAssets);
            }
        }

        private void DrawActions()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Use Selection"))
                    PullFromSelection();

                if (VisionEditorGui.PrimaryButton("Validate"))
                    Validate();

                if (GUILayout.Button("Clear"))
                    ClearReports();
            }
        }

        private void PullFromSelection()
        {
            if (Selection.activeObject is VisionModelProfile selectedModel)
                modelProfile = selectedModel;
            else if (Selection.activeObject is VisionPipelineProfile selectedPipeline)
                pipelineProfile = selectedPipeline;
        }

        private void Validate()
        {
            modelReport = modelProfile != null
                ? VisionProfileValidator.ValidateModelProfile(modelProfile, requireRuntimeAssets)
                : null;

            pipelineReport = pipelineProfile != null
                ? VisionProfileValidator.ValidatePipelineProfile(pipelineProfile, requireRuntimeAssets)
                : null;
        }

        private void ClearReports()
        {
            modelReport = null;
            pipelineReport = null;
        }

        private static void DrawReport(string title, VisionProfileValidationReport report)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            if (report == null)
            {
                EditorGUILayout.HelpBox("No validation run yet.", MessageType.None);
                return;
            }

            MessageType summaryType = report.IsValid
                ? (report.WarningCount > 0 ? MessageType.Warning : MessageType.Info)
                : MessageType.Error;
            EditorGUILayout.HelpBox(
                $"Errors: {report.ErrorCount}   Warnings: {report.WarningCount}   Messages: {report.Messages.Count}",
                summaryType);

            foreach (VisionValidationMessage message in report.Messages)
                EditorGUILayout.HelpBox($"{message.code}\n{message.message}", ToMessageType(message.severity));
        }

        private static MessageType ToMessageType(VisionValidationSeverity severity)
        {
            return severity switch
            {
                VisionValidationSeverity.Error => MessageType.Error,
                VisionValidationSeverity.Warning => MessageType.Warning,
                _ => MessageType.Info
            };
        }
    }
}
