using Unity.InferenceEngine;
using UnityEditor;
using UnityEngine;
using UniversalTracker.Core;

namespace UniversalTracker.Editor
{
    public sealed class VisionProfileWizardWindow : EditorWindow
    {
        private VisionModelProfileTemplate template = VisionModelProfileTemplate.YoloDetection;
        private string profileId;
        private string displayName;
        private ModelAsset modelAsset;
        private TextAsset labels;
        private BackendType backend = BackendType.CPU;
        private int inputSize = 640;
        private float confidenceThreshold = 0.5f;
        private float nmsThreshold = 0.45f;
        private string modelLicense = "Add model license before production";
        private string modelSourceUrl = "Add model source URL before production";
        private VisionProfileValidationReport previewReport;
        private Vector2 scroll;

        [MenuItem("Tools/ProAnima Vision/Advanced/Profile Wizard")]
        public static void Open()
        {
            var window = GetWindow<VisionProfileWizardWindow>("Vision Wizard");
            window.minSize = new Vector2(500f, 520f);
            window.ResetDefaults();
            window.Show();
        }

        private void OnGUI()
        {
            DrawHeader();

            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawTemplate();
            DrawRuntime();
            DrawGovernance();
            DrawActions();
            DrawPreview();
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            VisionEditorGui.DrawHeader(
                "ProAnima Vision Profile Wizard",
                "Create model profiles with consistent schemas, capabilities, thresholds, and parser metadata.");
        }

        private void DrawTemplate()
        {
            using (VisionEditorGui.Section("Template"))
            {
                EditorGUI.BeginChangeCheck();
                template = (VisionModelProfileTemplate)EditorGUILayout.EnumPopup("Template", template);
                if (EditorGUI.EndChangeCheck())
                    ResetDefaults();

                profileId = EditorGUILayout.TextField("Profile ID", profileId);
                displayName = EditorGUILayout.TextField("Display Name", displayName);
            }
        }

        private void DrawRuntime()
        {
            using (VisionEditorGui.Section("Runtime"))
            {
                modelAsset = (ModelAsset)EditorGUILayout.ObjectField("Model Asset", modelAsset, typeof(ModelAsset), false);
                labels = (TextAsset)EditorGUILayout.ObjectField("Labels", labels, typeof(TextAsset), false);
                backend = (BackendType)EditorGUILayout.EnumPopup("Backend", backend);
                inputSize = EditorGUILayout.IntSlider("Input Size", inputSize, 32, 2048);
                confidenceThreshold = EditorGUILayout.Slider("Confidence", confidenceThreshold, 0.01f, 0.99f);
                nmsThreshold = EditorGUILayout.Slider("NMS", nmsThreshold, 0.01f, 0.99f);
            }
        }

        private void DrawGovernance()
        {
            using (VisionEditorGui.Section("Governance"))
            {
                modelLicense = EditorGUILayout.TextField("Model License", modelLicense);
                modelSourceUrl = EditorGUILayout.TextField("Source URL", modelSourceUrl);
            }
        }

        private void DrawActions()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Preview Validation"))
                    PreviewValidation();

                if (VisionEditorGui.PrimaryButton("Create Profile"))
                    CreateProfile();
            }
        }

        private void DrawPreview()
        {
            if (previewReport == null)
                return;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Validation Preview", EditorStyles.boldLabel);
            MessageType summaryType = previewReport.IsValid
                ? (previewReport.WarningCount > 0 ? MessageType.Warning : MessageType.Info)
                : MessageType.Error;
            EditorGUILayout.HelpBox(
                $"{previewReport.Summary}\nErrors: {previewReport.ErrorCount}   Warnings: {previewReport.WarningCount}   Messages: {previewReport.Messages.Count}",
                summaryType);

            foreach (VisionValidationMessage message in previewReport.Messages)
                EditorGUILayout.HelpBox($"{message.code}\n{message.message}", ToMessageType(message.severity));
        }

        private void ResetDefaults()
        {
            VisionModelProfileTemplateSettings defaults = VisionModelProfileTemplateSettings.Defaults(template);
            profileId = defaults.profileId;
            displayName = defaults.displayName;
            modelAsset = defaults.modelAsset;
            labels = defaults.labels;
            backend = defaults.backend;
            inputSize = defaults.inputSize;
            confidenceThreshold = defaults.confidenceThreshold;
            nmsThreshold = defaults.nmsThreshold;
            modelLicense = defaults.modelLicense;
            modelSourceUrl = defaults.modelSourceUrl;
            previewReport = null;
        }

        private void PreviewValidation()
        {
            VisionModelProfile profile = CreateTransientProfile();
            previewReport = VisionProfileValidator.ValidateModelProfile(profile, requireRuntimeAsset: false);
            DestroyImmediate(profile);
        }

        private void CreateProfile()
        {
            VisionModelProfile profile = CreateTransientProfile();
            VisionProfileAssetCreator.CreateAsset(profile, VisionModelProfileTemplateFactory.DefaultAssetName(template));
            previewReport = VisionProfileValidator.ValidateModelProfile(profile, requireRuntimeAsset: false);
        }

        private VisionModelProfile CreateTransientProfile()
        {
            var settings = new VisionModelProfileTemplateSettings(
                profileId,
                displayName,
                modelAsset,
                labels,
                backend,
                inputSize,
                confidenceThreshold,
                nmsThreshold,
                modelLicense,
                modelSourceUrl);

            return VisionModelProfileTemplateFactory.Create(template, settings);
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
