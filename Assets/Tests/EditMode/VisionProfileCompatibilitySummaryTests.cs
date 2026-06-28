using NUnit.Framework;
using UnityEngine;
using UniversalTracker.Core;
using UniversalTracker.Editor;

namespace UniversalTracker.Tests
{
    public sealed class VisionProfileCompatibilitySummaryTests
    {
        [Test]
        public void FromModel_ForYoloTemplate_ReportsParserRuntimeAndSchema()
        {
            VisionModelProfile profile = VisionModelProfileTemplateFactory.Create(
                VisionModelProfileTemplate.YoloPose2D,
                VisionModelProfileTemplateSettings.Defaults(VisionModelProfileTemplate.YoloPose2D));

            try
            {
                VisionProfileCompatibilitySummary summary =
                    VisionProfileCompatibilitySummary.FromModel(profile, requireRuntimeAssets: false);

                Assert.That(summary.IsCompatible, Is.True);
                Assert.That(summary.model, Does.Contain("YOLO"));
                Assert.That(summary.runtime, Does.Contain("UnityInferenceEngine"));
                Assert.That(summary.parser, Does.Contain("yolo.pose2d.rows"));
                Assert.That(summary.capabilities, Does.Contain("Pose2D"));
                Assert.That(summary.input, Does.Contain("640x640x3"));
                Assert.That(summary.outputs, Does.Contain("output0"));
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void FromPipeline_ReturnsOneSummaryPerModel()
        {
            VisionModelProfile detection = VisionModelProfileTemplateFactory.Create(
                VisionModelProfileTemplate.YoloDetection,
                VisionModelProfileTemplateSettings.Defaults(VisionModelProfileTemplate.YoloDetection));
            VisionModelProfile segmentation = VisionModelProfileTemplateFactory.Create(
                VisionModelProfileTemplate.YoloSegmentation,
                VisionModelProfileTemplateSettings.Defaults(VisionModelProfileTemplate.YoloSegmentation));
            VisionPipelineProfile pipeline = VisionModelProfileTemplateFactory.CreatePipelineProfile(new[] { detection, segmentation });

            try
            {
                var summaries = VisionProfileCompatibilitySummary.FromPipeline(pipeline, requireRuntimeAssets: false);

                Assert.That(summaries.Count, Is.EqualTo(2));
                Assert.That(summaries[0].parser, Does.Contain("yolo.detection.rows"));
                Assert.That(summaries[1].parser, Does.Contain("yolo.segmentation.rows"));
            }
            finally
            {
                Object.DestroyImmediate(pipeline);
                Object.DestroyImmediate(detection);
                Object.DestroyImmediate(segmentation);
            }
        }
    }
}
