using System;
using System.Collections.Generic;
using System.Text;
using UniversalTracker.Core;

namespace UniversalTracker.Editor
{
    internal sealed class VisionProfileCompatibilitySummary
    {
        public string title;
        public string model;
        public string runtime;
        public string parser;
        public string capabilities;
        public string input;
        public string outputs;
        public string validationSummary;
        public bool isRuntimeReady;
        public VisionProfileValidationReport validationReport;

        public bool IsCompatible => validationReport != null && validationReport.IsValid;

        public static VisionProfileCompatibilitySummary FromModel(
            VisionModelProfile profile,
            bool requireRuntimeAssets,
            VisionOutputParserRegistry parserRegistry = null)
        {
            parserRegistry ??= VisionOutputParserRegistry.CreateDefault();
            VisionProfileValidationReport report = VisionProfileValidator.ValidateModelProfile(profile, requireRuntimeAssets, parserRegistry);

            if (profile == null)
                return NullSummary(report);

            string parserLabel = ResolveParserLabel(profile, parserRegistry);
            return new VisionProfileCompatibilitySummary
            {
                title = string.IsNullOrWhiteSpace(profile.displayName) ? profile.name : profile.displayName,
                model = $"{profile.family} / {profile.primaryTask}",
                runtime = $"{profile.runtimeKind} / {profile.sourceFormat} / {profile.backend}",
                parser = parserLabel,
                capabilities = profile.capabilities.ToString(),
                input = FormatInput(profile.input),
                outputs = FormatOutputs(profile.output),
                validationSummary = report.Summary,
                isRuntimeReady = report.IsValid,
                validationReport = report
            };
        }

        public static IReadOnlyList<VisionProfileCompatibilitySummary> FromPipeline(
            VisionPipelineProfile profile,
            bool requireRuntimeAssets,
            VisionOutputParserRegistry parserRegistry = null)
        {
            if (profile == null || profile.models == null || profile.models.Length == 0)
                return Array.Empty<VisionProfileCompatibilitySummary>();

            var summaries = new List<VisionProfileCompatibilitySummary>(profile.models.Length);
            for (int i = 0; i < profile.models.Length; i++)
                summaries.Add(FromModel(profile.models[i], requireRuntimeAssets, parserRegistry));
            return summaries;
        }

        private static VisionProfileCompatibilitySummary NullSummary(VisionProfileValidationReport report)
        {
            return new VisionProfileCompatibilitySummary
            {
                title = "No model profile",
                model = "-",
                runtime = "-",
                parser = "-",
                capabilities = "-",
                input = "-",
                outputs = "-",
                validationSummary = report.Summary,
                isRuntimeReady = false,
                validationReport = report
            };
        }

        private static string ResolveParserLabel(VisionModelProfile profile, VisionOutputParserRegistry parserRegistry)
        {
            if (parserRegistry.TryGetParser(profile, out IVisionOutputParser parser, out string code, out string message))
                return $"{parser.ParserId} ({parser.Capabilities})";

            return string.IsNullOrWhiteSpace(message) ? code : $"{code}: {message}";
        }

        private static string FormatInput(VisionInputSchema input)
        {
            if (!input.IsValid)
                return "Invalid input schema";

            string name = string.IsNullOrWhiteSpace(input.inputName) ? "unnamed" : input.inputName;
            return $"{name}: {input.width}x{input.height}x{input.channels}, aspect={input.preserveAspectRatio}, normalize={input.normalize}";
        }

        private static string FormatOutputs(VisionOutputSchema output)
        {
            if (!output.IsValid || output.tensors == null)
                return "No output schema";

            var builder = new StringBuilder();
            for (int i = 0; i < output.TensorCount; i++)
            {
                if (i > 0)
                    builder.Append("; ");

                VisionTensorSchema tensor = output.tensors[i];
                string shape = tensor.shape == null ? "?" : string.Join("x", tensor.shape);
                builder.Append(tensor.name).Append(" [").Append(shape).Append("] ").Append(tensor.semantic);
            }

            return builder.ToString();
        }
    }
}
