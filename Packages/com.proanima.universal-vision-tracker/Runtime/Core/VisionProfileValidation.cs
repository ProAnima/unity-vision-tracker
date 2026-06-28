using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniversalTracker.Core
{
    public enum VisionValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    public sealed class VisionValidationMessage
    {
        public VisionValidationSeverity severity;
        public string code;
        public string message;

        public VisionValidationMessage(VisionValidationSeverity severity, string code, string message)
        {
            this.severity = severity;
            this.code = code;
            this.message = message;
        }
    }

    public sealed class VisionProfileValidationReport
    {
        private readonly List<VisionValidationMessage> messages = new List<VisionValidationMessage>();

        public IReadOnlyList<VisionValidationMessage> Messages => messages;
        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }
        public bool IsValid => ErrorCount == 0;

        public void Add(VisionValidationSeverity severity, string code, string message)
        {
            messages.Add(new VisionValidationMessage(severity, code, message));

            if (severity == VisionValidationSeverity.Error)
                ErrorCount++;
            else if (severity == VisionValidationSeverity.Warning)
                WarningCount++;
        }

        public string ToLogString()
        {
            if (messages.Count == 0)
                return "Validation passed.";

            var lines = new string[messages.Count];
            for (int i = 0; i < messages.Count; i++)
            {
                var item = messages[i];
                lines[i] = $"{item.severity}: {item.code} - {item.message}";
            }

            return string.Join(Environment.NewLine, lines);
        }
    }

    public static class VisionProfileValidator
    {
        public static VisionProfileValidationReport ValidateModelProfile(
            VisionModelProfile profile,
            bool requireRuntimeAsset = true,
            VisionOutputParserRegistry parserRegistry = null)
        {
            var report = new VisionProfileValidationReport();

            if (profile == null)
            {
                report.Add(VisionValidationSeverity.Error, "model.null", "VisionModelProfile is null.");
                return report;
            }

            if (string.IsNullOrWhiteSpace(profile.profileId))
                report.Add(VisionValidationSeverity.Warning, "model.profile_id.empty", "profileId is empty; stable IDs are required for presets, telemetry, and migrations.");

            if (string.IsNullOrWhiteSpace(profile.displayName))
                report.Add(VisionValidationSeverity.Warning, "model.display_name.empty", "displayName is empty; inspectors and runtime UI will fall back to asset names.");

            if (profile.family == VisionModelFamily.Unknown)
                report.Add(VisionValidationSeverity.Error, "model.family.unknown", "Model family must be set to a concrete adapter family.");

            if (profile.capabilities == VisionModelCapability.None)
                report.Add(VisionValidationSeverity.Error, "model.capabilities.empty", "At least one model capability must be declared.");

            ValidateTaskCapability(profile, report);
            ValidateRuntime(profile, requireRuntimeAsset, report);
            ValidateSchemas(profile, report);
            ValidateParser(profile, parserRegistry ?? VisionOutputParserRegistry.CreateDefault(), report);
            ValidateThresholds(profile, report);
            ValidateGovernance(profile, report);

            return report;
        }

        public static VisionProfileValidationReport ValidatePipelineProfile(VisionPipelineProfile profile, bool requireRuntimeAssets = true)
        {
            var report = new VisionProfileValidationReport();

            if (profile == null)
            {
                report.Add(VisionValidationSeverity.Error, "pipeline.null", "VisionPipelineProfile is null.");
                return report;
            }

            if (!profile.HasModels)
            {
                report.Add(VisionValidationSeverity.Error, "pipeline.models.empty", "Pipeline profile must contain at least one model.");
                return report;
            }

            if (profile.defaultModelIndex < 0 || profile.defaultModelIndex >= profile.models.Length)
                report.Add(VisionValidationSeverity.Warning, "pipeline.default_model.clamped", "defaultModelIndex is outside the model array and will be clamped at runtime.");

            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < profile.models.Length; i++)
            {
                var model = profile.models[i];
                var modelReport = ValidateModelProfile(model, requireRuntimeAssets);
                CopyMessages(modelReport, report, $"pipeline.models[{i}].");

                if (model == null || string.IsNullOrWhiteSpace(model.profileId))
                    continue;

                if (!seenIds.Add(model.profileId))
                    report.Add(VisionValidationSeverity.Warning, $"pipeline.models[{i}].profile_id.duplicate", $"Duplicate profileId '{model.profileId}' can make migrations and telemetry ambiguous.");
            }

            return report;
        }

        private static void ValidateTaskCapability(VisionModelProfile profile, VisionProfileValidationReport report)
        {
            switch (profile.primaryTask)
            {
                case VisionTaskType.Detection:
                    RequireCapability(profile, VisionModelCapability.Detection, "task.detection.requires_capability", report);
                    break;
                case VisionTaskType.HumanDetection:
                    RequireCapability(profile, VisionModelCapability.HumanDetection, "task.human_detection.requires_capability", report);
                    break;
                case VisionTaskType.Pose:
                    if (!profile.Supports(VisionModelCapability.Pose2D) && !profile.Supports(VisionModelCapability.Pose3D))
                        report.Add(VisionValidationSeverity.Error, "task.pose.requires_capability", "Pose task requires Pose2D or Pose3D capability.");
                    break;
                case VisionTaskType.Segmentation:
                    RequireCapability(profile, VisionModelCapability.Segmentation, "task.segmentation.requires_capability", report);
                    break;
                case VisionTaskType.Classification:
                    RequireCapability(profile, VisionModelCapability.Classification, "task.classification.requires_capability", report);
                    break;
            }
        }

        private static void ValidateRuntime(VisionModelProfile profile, bool requireRuntimeAsset, VisionProfileValidationReport report)
        {
            if (profile.runtimeKind == VisionRuntimeKind.UnityInferenceEngine && profile.sourceFormat != VisionModelSourceFormat.UnityModelAsset)
                report.Add(VisionValidationSeverity.Error, "runtime.unity_inference.requires_model_asset_format", "Unity Inference Engine runtime requires UnityModelAsset source format.");

            if (requireRuntimeAsset && profile.sourceFormat == VisionModelSourceFormat.UnityModelAsset && profile.modelAsset == null)
                report.Add(VisionValidationSeverity.Error, "runtime.model_asset.missing", "UnityModelAsset source format requires a ModelAsset reference.");

            if (profile.runtimeKind == VisionRuntimeKind.Custom && string.IsNullOrWhiteSpace(profile.parserId))
                report.Add(VisionValidationSeverity.Warning, "runtime.custom.parser_id.empty", "Custom runtimes should declare parserId so adapters can resolve output schemas.");
        }

        private static void ValidateSchemas(VisionModelProfile profile, VisionProfileValidationReport report)
        {
            if (!profile.input.IsValid)
                report.Add(VisionValidationSeverity.Error, "schema.input.invalid", "Input schema width, height, and channels must be greater than zero.");

            if (profile.input.width != profile.input.height && profile.family == VisionModelFamily.YOLO)
                report.Add(VisionValidationSeverity.Warning, "schema.input.yolo_non_square", "YOLO profiles usually expect square input; verify preprocessing and output mapping.");

            if (!profile.output.IsValid)
                report.Add(VisionValidationSeverity.Warning, "schema.output.empty", "Output schema is empty; parser compatibility cannot be validated ahead of runtime.");
            else
            {
                for (int i = 0; i < profile.output.TensorCount; i++)
                {
                    if (!profile.output.tensors[i].IsValid)
                        report.Add(VisionValidationSeverity.Error, $"schema.output.tensors[{i}].invalid", "Output tensor must have a name and shape.");
                }
            }
        }

        private static void ValidateParser(VisionModelProfile profile, VisionOutputParserRegistry parserRegistry, VisionProfileValidationReport report)
        {
            if (parserRegistry == null)
            {
                report.Add(VisionValidationSeverity.Error, "parser.registry.null", "Parser registry is null; parser compatibility cannot be validated.");
                return;
            }

            if (!parserRegistry.TryGetParser(profile, out IVisionOutputParser parser, out string code, out string message))
            {
                report.Add(VisionValidationSeverity.Error, code ?? "parser.resolve.failed", message ?? "No output parser could be resolved for this model profile.");
                return;
            }

            if ((profile.capabilities & parser.Capabilities) != parser.Capabilities)
            {
                report.Add(
                    VisionValidationSeverity.Error,
                    "parser.capabilities.unsupported",
                    $"Parser '{parser.ParserId}' requires capabilities '{parser.Capabilities}', but profile declares '{profile.capabilities}'.");
                return;
            }

            if (!parser.CanParse(profile))
            {
                report.Add(
                    VisionValidationSeverity.Error,
                    "parser.profile.unsupported",
                    $"Parser '{parser.ParserId}' is registered but does not support this model family/task/profile combination.");
                return;
            }

            report.Add(VisionValidationSeverity.Info, "parser.selected", $"Output parser '{parser.ParserId}' is compatible with this profile.");
            ValidateParserSchemaSemantics(profile, parser, report);
        }

        private static void ValidateParserSchemaSemantics(VisionModelProfile profile, IVisionOutputParser parser, VisionProfileValidationReport report)
        {
            if (!profile.output.IsValid || profile.output.tensors == null)
                return;

            if ((parser.Capabilities & VisionModelCapability.Detection) == VisionModelCapability.Detection &&
                !HasTensorSemantic(profile.output, "detection"))
            {
                report.Add(
                    VisionValidationSeverity.Warning,
                    "parser.output.semantic.detection_missing",
                    $"Parser '{parser.ParserId}' handles detections, but no output tensor semantic contains 'detection'.");
            }
        }

        private static bool HasTensorSemantic(VisionOutputSchema output, string semanticFragment)
        {
            for (int i = 0; i < output.TensorCount; i++)
            {
                string semantic = output.tensors[i].semantic;
                if (!string.IsNullOrWhiteSpace(semantic) &&
                    semantic.IndexOf(semanticFragment, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ValidateThresholds(VisionModelProfile profile, VisionProfileValidationReport report)
        {
            if (profile.confidenceThreshold <= 0f || profile.confidenceThreshold >= 1f)
                report.Add(VisionValidationSeverity.Error, "threshold.confidence.out_of_range", "confidenceThreshold must be between 0 and 1.");

            if (profile.nmsThreshold <= 0f || profile.nmsThreshold >= 1f)
                report.Add(VisionValidationSeverity.Error, "threshold.nms.out_of_range", "nmsThreshold must be between 0 and 1.");
        }

        private static void ValidateGovernance(VisionModelProfile profile, VisionProfileValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(profile.modelLicense))
                report.Add(VisionValidationSeverity.Warning, "governance.license.empty", "modelLicense is empty; production packages should preserve model usage terms.");

            if (string.IsNullOrWhiteSpace(profile.modelSourceUrl))
                report.Add(VisionValidationSeverity.Warning, "governance.source_url.empty", "modelSourceUrl is empty; production packages should preserve provenance.");
        }

        private static void RequireCapability(VisionModelProfile profile, VisionModelCapability capability, string code, VisionProfileValidationReport report)
        {
            if (!profile.Supports(capability))
                report.Add(VisionValidationSeverity.Error, code, $"{profile.primaryTask} task requires {capability} capability.");
        }

        private static void CopyMessages(VisionProfileValidationReport source, VisionProfileValidationReport target, string prefix)
        {
            foreach (var message in source.Messages)
                target.Add(message.severity, prefix + message.code, message.message);
        }
    }
}
