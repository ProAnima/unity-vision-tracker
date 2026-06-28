using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UniversalTracker.Core;

namespace UniversalTracker.Tests
{
    public sealed class VisionProfileValidationTests
    {
        [Test]
        public void ValidateModelProfile_ValidProfileWithoutRuntimeAsset_Passes()
        {
            var profile = CreateValidProfile();

            VisionProfileValidationReport report = VisionProfileValidator.ValidateModelProfile(profile, false);

            Assert.That(report.IsValid, Is.True);
            Assert.That(report.ErrorCount, Is.EqualTo(0));
            Assert.That(report.Messages.Any(message => message.code == "parser.selected"), Is.True);

            Object.DestroyImmediate(profile);
        }

        [Test]
        public void ValidateModelProfile_MissingRuntimeAsset_FailsWhenRequired()
        {
            var profile = CreateValidProfile();

            VisionProfileValidationReport report = VisionProfileValidator.ValidateModelProfile(profile);

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Messages.Any(message => message.code == "runtime.model_asset.missing"), Is.True);

            Object.DestroyImmediate(profile);
        }

        [Test]
        public void ValidateModelProfile_TaskCapabilityMismatch_Fails()
        {
            var profile = CreateValidProfile();
            profile.primaryTask = VisionTaskType.Pose;
            profile.capabilities = VisionModelCapability.Detection;

            VisionProfileValidationReport report = VisionProfileValidator.ValidateModelProfile(profile, false);

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Messages.Any(message => message.code == "task.pose.requires_capability"), Is.True);

            Object.DestroyImmediate(profile);
        }

        [Test]
        public void ValidatePipelineProfile_DetectsDuplicateIdsAndInvalidDefaultIndex()
        {
            var first = CreateValidProfile();
            var second = CreateValidProfile();
            second.profileId = first.profileId;
            var pipeline = ScriptableObject.CreateInstance<VisionPipelineProfile>();
            pipeline.models = new[] { first, second };
            pipeline.defaultModelIndex = 99;

            VisionProfileValidationReport report = VisionProfileValidator.ValidatePipelineProfile(pipeline, false);

            Assert.That(report.IsValid, Is.True);
            Assert.That(report.WarningCount, Is.GreaterThanOrEqualTo(2));
            Assert.That(report.Messages.Any(message => message.code == "pipeline.default_model.clamped"), Is.True);
            Assert.That(report.Messages.Any(message => message.code == "pipeline.models[1].profile_id.duplicate"), Is.True);

            Object.DestroyImmediate(pipeline);
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
        }

        [Test]
        public void ValidateModelProfile_UnknownParserId_FailsWithDiagnostic()
        {
            var profile = CreateValidProfile();
            profile.parserId = "missing.parser";

            VisionProfileValidationReport report = VisionProfileValidator.ValidateModelProfile(profile, false);

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Messages.Any(message => message.code == "parser.id.not_registered"), Is.True);

            Object.DestroyImmediate(profile);
        }

        [Test]
        public void ValidateModelProfile_NoParserForCapability_FailsWithDiagnostic()
        {
            var profile = CreateValidProfile();
            profile.family = VisionModelFamily.Custom;
            profile.parserId = null;

            VisionProfileValidationReport report = VisionProfileValidator.ValidateModelProfile(profile, false);

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Messages.Any(message => message.code == "parser.compatibility.none"), Is.True);

            Object.DestroyImmediate(profile);
        }

        [Test]
        public void ValidateModelProfile_ParserIdForUnsupportedFamily_FailsWithDiagnostic()
        {
            var profile = CreateValidProfile();
            profile.family = VisionModelFamily.MediaPipe;
            profile.parserId = "yolo.detection.rows";

            VisionProfileValidationReport report = VisionProfileValidator.ValidateModelProfile(profile, false);

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Messages.Any(message => message.code == "parser.profile.unsupported"), Is.True);

            Object.DestroyImmediate(profile);
        }

        [Test]
        public void ValidationReport_ToLogString_IncludesSeverityCodeAndMessage()
        {
            var report = new VisionProfileValidationReport();
            report.Add(VisionValidationSeverity.Error, "sample.error", "Something is wrong.");

            string log = report.ToLogString();

            Assert.That(log, Does.Contain("Error"));
            Assert.That(log, Does.Contain("sample.error"));
            Assert.That(log, Does.Contain("Something is wrong."));
        }

        [Test]
        public void ValidationReport_Summary_DescribesErrorsWarningsAndCompatibility()
        {
            var warningReport = new VisionProfileValidationReport();
            warningReport.Add(VisionValidationSeverity.Warning, "sample.warning", "Review metadata.");
            var errorReport = new VisionProfileValidationReport();
            errorReport.Add(VisionValidationSeverity.Error, "sample.error", "Fix profile.");
            var validReport = new VisionProfileValidationReport();
            validReport.Add(VisionValidationSeverity.Info, "parser.selected", "Parser selected.");

            Assert.That(warningReport.Summary, Does.Contain("warning"));
            Assert.That(errorReport.Summary, Does.Contain("Fix errors"));
            Assert.That(validReport.Summary, Is.EqualTo("Profile is compatible."));
        }

        private static VisionModelProfile CreateValidProfile()
        {
            var profile = ScriptableObject.CreateInstance<VisionModelProfile>();
            profile.profileId = "yolo-detector";
            profile.displayName = "YOLO Detector";
            profile.family = VisionModelFamily.YOLO;
            profile.primaryTask = VisionTaskType.Detection;
            profile.capabilities = VisionModelCapability.Detection;
            profile.runtimeKind = VisionRuntimeKind.UnityInferenceEngine;
            profile.sourceFormat = VisionModelSourceFormat.UnityModelAsset;
            profile.input = VisionInputSchema.ImageSquare(640);
            profile.output = new VisionOutputSchema
            {
                tensors = new[]
                {
                    new VisionTensorSchema
                    {
                        name = "output0",
                        shape = new[] { 1, 84, 8400 },
                        semantic = "detections"
                    }
                }
            };
            profile.parserId = "yolo.detection.rows";
            profile.confidenceThreshold = 0.5f;
            profile.nmsThreshold = 0.45f;
            profile.modelLicense = "MIT";
            profile.modelSourceUrl = "https://example.com/model";
            return profile;
        }
    }
}
