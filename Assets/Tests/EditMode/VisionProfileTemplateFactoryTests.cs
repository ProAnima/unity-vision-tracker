using NUnit.Framework;
using Unity.InferenceEngine;
using UnityEngine;
using UniversalTracker.Core;
using UniversalTracker.Editor;

namespace UniversalTracker.Tests
{
    public sealed class VisionProfileTemplateFactoryTests
    {
        [Test]
        public void YoloDetectionTemplate_CreatesValidatorCompatibleProfileWithoutRuntimeAssetRequirement()
        {
            VisionModelProfile profile = VisionModelProfileTemplateFactory.Create(
                VisionModelProfileTemplate.YoloDetection,
                VisionModelProfileTemplateSettings.Defaults(VisionModelProfileTemplate.YoloDetection));

            VisionProfileValidationReport report = VisionProfileValidator.ValidateModelProfile(profile, requireRuntimeAsset: false);

            Assert.That(profile.family, Is.EqualTo(VisionModelFamily.YOLO));
            Assert.That(profile.primaryTask, Is.EqualTo(VisionTaskType.Detection));
            Assert.That(profile.capabilities, Is.EqualTo(VisionModelCapability.Detection));
            Assert.That(profile.parserId, Is.EqualTo("yolo.detection.rows"));
            Assert.That(profile.backend, Is.EqualTo(BackendType.CPU));
            Assert.That(profile.output.TensorCount, Is.EqualTo(1));
            Assert.That(report.IsValid, Is.True);

            Object.DestroyImmediate(profile);
        }

        [Test]
        public void YoloPoseTemplate_DeclaresPoseCapabilityAndSchema()
        {
            VisionModelProfile profile = VisionModelProfileTemplateFactory.Create(
                VisionModelProfileTemplate.YoloPose2D,
                VisionModelProfileTemplateSettings.Defaults(VisionModelProfileTemplate.YoloPose2D));

            Assert.That(profile.primaryTask, Is.EqualTo(VisionTaskType.Pose));
            Assert.That(profile.Supports(VisionModelCapability.Pose2D), Is.True);
            Assert.That(profile.Supports(VisionModelCapability.HumanDetection), Is.True);
            Assert.That(profile.parserId, Is.EqualTo("yolo.pose2d.rows"));
            Assert.That(profile.output.TensorCount, Is.EqualTo(1));
            Assert.That(profile.output.tensors[0].semantic, Is.EqualTo("pose.rows"));

            Object.DestroyImmediate(profile);
        }

        [Test]
        public void YoloSegmentationTemplate_DeclaresMaskPrototypeSchema()
        {
            VisionModelProfile profile = VisionModelProfileTemplateFactory.Create(
                VisionModelProfileTemplate.YoloSegmentation,
                VisionModelProfileTemplateSettings.Defaults(VisionModelProfileTemplate.YoloSegmentation));

            Assert.That(profile.primaryTask, Is.EqualTo(VisionTaskType.Segmentation));
            Assert.That(profile.Supports(VisionModelCapability.Segmentation), Is.True);
            Assert.That(profile.parserId, Is.EqualTo("yolo.segmentation.rows"));
            Assert.That(profile.output.TensorCount, Is.EqualTo(2));
            Assert.That(profile.output.tensors[1].semantic, Is.EqualTo("segmentation.prototype"));

            Object.DestroyImmediate(profile);
        }

        [Test]
        public void PipelineTemplate_PreservesModelOrder()
        {
            var first = ScriptableObject.CreateInstance<VisionModelProfile>();
            var second = ScriptableObject.CreateInstance<VisionModelProfile>();

            VisionPipelineProfile pipeline = VisionModelProfileTemplateFactory.CreatePipelineProfile(new[] { first, second });

            Assert.That(pipeline.models, Is.EqualTo(new[] { first, second }));
            Assert.That(pipeline.defaultModelIndex, Is.EqualTo(0));
            Assert.That(pipeline.enableTracking, Is.True);

            Object.DestroyImmediate(pipeline);
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
        }
    }
}
