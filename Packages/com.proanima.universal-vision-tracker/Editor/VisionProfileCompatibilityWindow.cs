using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UniversalTracker.Core;

namespace UniversalTracker.Editor
{
    public sealed class VisionProfileCompatibilityWindow : EditorWindow
    {
        private VisionModelProfile modelProfile;
        private VisionPipelineProfile pipelineProfile;
        private bool requireRuntimeAssets = true;
        private Vector2 scroll;
        private IReadOnlyList<VisionProfileCompatibilitySummary> summaries;

        [MenuItem("Tools/ProAnima Vision/Profile Compatibility Inspector")]
        public static void Open()
        {
            var window = GetWindow<VisionProfileCompatibilityWindow>("Vision Compatibility");
            window.minSize = new Vector2(540f, 520f);
            window.PullFromSelection();
            window.Refresh();
            window.Show();
        }

        private void OnSelectionChange()
        {
            if (focusedWindow != this)
                return;

            PullFromSelection();
            Refresh();
            Repaint();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawTargets();
            DrawActions();

            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawSummaries();
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("ProAnima Vision Profile Compatibility", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Inspect runtime, parser, capability, input, and output-schema compatibility before putting a profile into a scene.",
                MessageType.Info);
        }

        private void DrawTargets()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
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
                {
                    PullFromSelection();
                    Refresh();
                }

                if (GUILayout.Button("Refresh"))
                    Refresh();
            }
        }

        private void PullFromSelection()
        {
            if (Selection.activeObject is VisionModelProfile selectedModel)
                modelProfile = selectedModel;
            else if (Selection.activeObject is VisionPipelineProfile selectedPipeline)
                pipelineProfile = selectedPipeline;
        }

        private void Refresh()
        {
            if (pipelineProfile != null)
                summaries = VisionProfileCompatibilitySummary.FromPipeline(pipelineProfile, requireRuntimeAssets);
            else
                summaries = new[] { VisionProfileCompatibilitySummary.FromModel(modelProfile, requireRuntimeAssets) };
        }

        private void DrawSummaries()
        {
            if (summaries == null || summaries.Count == 0)
            {
                EditorGUILayout.HelpBox("No model profiles to inspect.", MessageType.Warning);
                return;
            }

            for (int i = 0; i < summaries.Count; i++)
                DrawSummary(summaries[i], i);
        }

        private static void DrawSummary(VisionProfileCompatibilitySummary summary, int index)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"{index + 1}. {summary.title}", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(summary.validationSummary, summary.IsCompatible ? MessageType.Info : MessageType.Error);
                DrawRow("Model", summary.model);
                DrawRow("Runtime", summary.runtime);
                DrawRow("Parser", summary.parser);
                DrawRow("Capabilities", summary.capabilities);
                DrawRow("Input", summary.input);
                DrawRow("Outputs", summary.outputs);
            }
        }

        private static void DrawRow(string name, string value)
        {
            EditorGUILayout.LabelField(name, value, EditorStyles.wordWrappedLabel);
        }
    }
}
