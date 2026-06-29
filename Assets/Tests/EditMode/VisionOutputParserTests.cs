using NUnit.Framework;
using UnityEngine;
using UniversalTracker.Core;

namespace UniversalTracker.Tests
{
    public sealed class VisionOutputParserTests
    {
        [Test]
        public void YoloDetectionParser_ParsesRowFixtureToDetections()
        {
            var parser = new YoloDetectionOutputParser();
            VisionRawModelOutput raw = VisionRawModelOutput.Single(
                "output0",
                new[]
                {
                    0.50f, 0.50f, 0.20f, 0.40f, 0.90f, 0.10f, 0.80f,
                    0.20f, 0.25f, 0.10f, 0.10f, 0.70f, 0.90f, 0.05f
                },
                2,
                7);
            var context = new VisionOutputParserContext(
                new Vector2Int(1000, 500),
                0.25f,
                0.5f,
                new[] { "person", "tool" });

            VisionParsedOutput parsed = parser.Parse(raw, context);

            Assert.That(parsed.detections.Length, Is.EqualTo(2));
            Assert.That(parsed.detections[0].label, Is.EqualTo("tool"));
            Assert.That(parsed.detections[0].confidence, Is.EqualTo(0.72f).Within(0.0001f));
            AssertRect(parsed.detections[0].sourceRect, new Rect(400, 150, 200, 200));
            Assert.That(parsed.detections[1].label, Is.EqualTo("person"));
            Assert.That(parsed.detections[1].sourceCenter, Is.EqualTo(new Vector2(200, 125)));
        }

        [Test]
        public void YoloDetectionParser_FiltersLowConfidenceRows()
        {
            var parser = new YoloDetectionOutputParser();
            VisionRawModelOutput raw = VisionRawModelOutput.Single(
                "output0",
                new[] { 0.5f, 0.5f, 0.2f, 0.2f, 0.5f, 0.5f },
                1,
                6);
            var context = new VisionOutputParserContext(new Vector2Int(100, 100), 0.3f, 0.5f);

            VisionParsedOutput parsed = parser.Parse(raw, context);

            Assert.That(parsed.detections, Is.Empty);
        }

        [Test]
        public void YoloDetectionParser_ParsesChannelFirstYoloOutputWithoutObjectness()
        {
            var parser = new YoloDetectionOutputParser();
            float[] data = new float[84 * 2];
            WriteChannelFirstBox(data, 2, 0, 320f, 320f, 128f, 256f, classId: 0, score: 0.82f);
            WriteChannelFirstBox(data, 2, 1, 120f, 120f, 64f, 64f, classId: 2, score: 0.12f);
            VisionRawModelOutput raw = VisionRawModelOutput.Single("output0", data, 1, 84, 2);
            var context = new VisionOutputParserContext(
                new Vector2Int(1280, 720),
                0.25f,
                0.5f,
                new[] { "person", "bicycle", "car" },
                new Vector2Int(640, 640));

            VisionParsedOutput parsed = parser.Parse(raw, context);

            Assert.That(parsed.detections, Has.Length.EqualTo(1));
            Assert.That(parsed.detections[0].label, Is.EqualTo("person"));
            Assert.That(parsed.detections[0].confidence, Is.EqualTo(0.82f).Within(0.0001f));
            AssertRect(parsed.detections[0].sourceRect, new Rect(512, 216, 256, 288));
            Assert.That(parsed.diagnostics.modelOutput, Does.Contain("1x84x2"));
            Assert.That(parsed.diagnostics.candidateCount, Is.EqualTo(1));
            Assert.That(parsed.diagnostics.acceptedCount, Is.EqualTo(1));
            Assert.That(parsed.diagnostics.maxConfidence, Is.EqualTo(0.82f).Within(0.0001f));
        }

        [Test]
        public void YoloDetectionParser_UsesFirstCompatibleTensor()
        {
            var parser = new YoloDetectionOutputParser();
            var raw = new VisionRawModelOutput
            {
                tensors = new[]
                {
                    new VisionRawTensor("metadata", new[] { 1f, 2f, 3f }, new[] { 3 }),
                    new VisionRawTensor(
                        "detections",
                        new[] { 0.5f, 0.5f, 0.2f, 0.2f, 0.9f, 0.8f },
                        new[] { 1, 6 })
                }
            };
            var context = new VisionOutputParserContext(new Vector2Int(100, 100), 0.25f, 0.5f, new[] { "person" });

            VisionParsedOutput parsed = parser.Parse(raw, context);

            Assert.That(parsed.detections, Has.Length.EqualTo(1));
            Assert.That(parsed.diagnostics.modelOutput, Does.Contain("detections"));
        }

        [Test]
        public void YoloDetectionParser_AppliesOutputCoordinateTransform()
        {
            var parser = new YoloDetectionOutputParser();
            float[] data = new float[84];
            WriteChannelFirstBox(data, 1, 0, 320f, 160f, 128f, 128f, classId: 0, score: 0.82f);
            VisionOutputCoordinateTransform transform = VisionOutputCoordinateTransform.Identity;
            transform.flipY = true;
            VisionRawModelOutput raw = VisionRawModelOutput.Single("output0", data, 1, 84, 1);
            var context = new VisionOutputParserContext(
                new Vector2Int(1280, 720),
                0.25f,
                0.5f,
                new[] { "person" },
                new Vector2Int(640, 640),
                transform);

            VisionParsedOutput parsed = parser.Parse(raw, context);

            Assert.That(parsed.detections, Has.Length.EqualTo(1));
            AssertRect(parsed.detections[0].sourceRect, new Rect(512, 468, 256, 144));
        }

        [Test]
        public void YoloDetectionParser_ParsesYolo26EndToEndOutput()
        {
            var parser = new YoloDetectionOutputParser();
            float[] data = new float[300 * 6];
            WriteYolo26Row(data, 6, 0, 100f, 50f, 300f, 250f, 0.82f, 0);
            VisionRawModelOutput raw = VisionRawModelOutput.Single("output0", data, 1, 300, 6);
            var context = new VisionOutputParserContext(
                new Vector2Int(1280, 720),
                0.25f,
                0.5f,
                new[] { "person" },
                new Vector2Int(640, 640));

            VisionParsedOutput parsed = parser.Parse(raw, context);

            Assert.That(parsed.detections, Has.Length.EqualTo(1));
            Assert.That(parsed.detections[0].label, Is.EqualTo("person"));
            AssertRect(parsed.detections[0].sourceRect, new Rect(200, 56.25f, 400, 225));
        }

        [Test]
        public void YoloDetectionParser_AppliesClassAwareNms()
        {
            var parser = new YoloDetectionOutputParser();
            VisionRawModelOutput raw = VisionRawModelOutput.Single(
                "output0",
                new[]
                {
                    0.50f, 0.50f, 0.40f, 0.40f, 0.95f, 0.90f, 0.01f,
                    0.52f, 0.50f, 0.40f, 0.40f, 0.90f, 0.85f, 0.01f,
                    0.52f, 0.50f, 0.40f, 0.40f, 0.90f, 0.01f, 0.85f
                },
                3,
                7);
            var context = new VisionOutputParserContext(new Vector2Int(100, 100), 0.2f, 0.5f);

            VisionParsedOutput parsed = parser.Parse(raw, context);

            Assert.That(parsed.detections.Length, Is.EqualTo(2));
            Assert.That(parsed.detections[0].classId, Is.EqualTo(0));
            Assert.That(parsed.detections[1].classId, Is.EqualTo(1));
        }

        [Test]
        public void YoloPoseParser_ParsesPoseFixtureToDetectionsAndKeypoints()
        {
            var parser = new YoloPose2DOutputParser();
            VisionRawModelOutput raw = VisionRawModelOutput.Single("output0", CreatePoseRow(), 1, 56);
            var context = new VisionOutputParserContext(new Vector2Int(1000, 500), 0.25f, 0.5f, new[] { "person" });

            VisionParsedOutput parsed = parser.Parse(raw, context);

            Assert.That(parsed.detections, Has.Length.EqualTo(1));
            Assert.That(parsed.poses, Has.Length.EqualTo(1));
            Assert.That(parsed.detections[0].label, Is.EqualTo("person"));
            AssertRect(parsed.detections[0].sourceRect, new Rect(400, 150, 200, 200));
            Assert.That(parsed.poses[0].keypoints, Has.Length.EqualTo(17));
            Assert.That(parsed.poses[0].keypoints[0].name, Is.EqualTo("nose"));
            Assert.That(parsed.poses[0].keypoints[0].sourcePosition, Is.EqualTo(new Vector2(500, 200)));
            Assert.That(parsed.poses[0].VisibleKeypointCount, Is.EqualTo(17));
            Assert.That(parsed.poses[0].skeleton.name, Is.EqualTo("COCO-17"));
        }

        [Test]
        public void YoloPoseParser_ParsesChannelFirstYolo11PoseOutput()
        {
            var parser = new YoloPose2DOutputParser();
            float[] data = new float[56];
            WriteChannelFirstPose(data, 1, 0, 320f, 160f, 128f, 128f, score: 0.76f);
            VisionRawModelOutput raw = VisionRawModelOutput.Single("pose", data, 1, 56, 1);
            var context = new VisionOutputParserContext(
                new Vector2Int(1280, 720),
                0.25f,
                0.5f,
                new[] { "person" },
                new Vector2Int(640, 640));

            VisionParsedOutput parsed = parser.Parse(raw, context);

            Assert.That(parsed.detections, Has.Length.EqualTo(1));
            Assert.That(parsed.poses, Has.Length.EqualTo(1));
            AssertRect(parsed.detections[0].sourceRect, new Rect(512, 108, 256, 144));
            Assert.That(parsed.poses[0].keypoints[0].sourcePosition, Is.EqualTo(new Vector2(640, 180)));
            Assert.That(parsed.diagnostics.modelOutput, Does.Contain("1x56x1"));
        }

        [Test]
        public void YoloPoseParser_ParsesYolo26EndToEndPoseOutput()
        {
            var parser = new YoloPose2DOutputParser();
            float[] data = new float[300 * 57];
            WriteYolo26Row(data, 57, 0, 100f, 50f, 300f, 250f, 0.82f, 0);
            WriteYolo26PoseKeypoints(data, 57, 0, 200f, 150f, 0.75f);
            VisionRawModelOutput raw = VisionRawModelOutput.Single("pose", data, 1, 300, 57);
            var context = new VisionOutputParserContext(
                new Vector2Int(1280, 720),
                0.25f,
                0.5f,
                new[] { "person" },
                new Vector2Int(640, 640));

            VisionParsedOutput parsed = parser.Parse(raw, context);

            Assert.That(parsed.detections, Has.Length.EqualTo(1));
            Assert.That(parsed.poses, Has.Length.EqualTo(1));
            AssertRect(parsed.detections[0].sourceRect, new Rect(200, 56.25f, 400, 225));
            Assert.That(parsed.poses[0].keypoints[0].sourcePosition, Is.EqualTo(new Vector2(400, 168.75f)));
        }

        [Test]
        public void YoloSegmentationParser_ParsesMaskFixtureToDetectionsAndMasks()
        {
            var parser = new YoloSegmentationOutputParser();
            var raw = new VisionRawModelOutput
            {
                tensors = new[]
                {
                    new VisionRawTensor(
                        "output0",
                        new[]
                        {
                            0.50f, 0.50f, 0.20f, 0.40f, 0.90f, 0.80f, 0.25f, -0.10f,
                            0.15f, 0.15f, 0.10f, 0.10f, 0.20f, 0.70f, 0.05f, 0.30f
                        },
                        new[] { 2, 8 }),
                    new VisionRawTensor("proto", new float[8], new[] { 1, 2, 2, 2 })
                }
            };
            var context = new VisionOutputParserContext(new Vector2Int(1000, 500), 0.25f, 0.5f, new[] { "person" });

            VisionParsedOutput parsed = parser.Parse(raw, context);

            Assert.That(parsed.detections, Has.Length.EqualTo(1));
            Assert.That(parsed.masks, Has.Length.EqualTo(1));
            Assert.That(parsed.masks[0].label, Is.EqualTo("person"));
            Assert.That(parsed.masks[0].confidence, Is.EqualTo(0.72f).Within(0.0001f));
            AssertRect(parsed.masks[0].sourceRect, new Rect(400, 150, 200, 200));
        }

        [Test]
        public void YoloSegmentationParser_ParsesChannelFirstYolo11SegmentationOutput()
        {
            var parser = new YoloSegmentationOutputParser();
            float[] data = new float[116];
            data[0] = 320f;
            data[1] = 160f;
            data[2] = 128f;
            data[3] = 128f;
            data[4] = 0.72f;
            var raw = new VisionRawModelOutput
            {
                tensors = new[]
                {
                    new VisionRawTensor("seg", data, new[] { 1, 116, 1 }),
                    new VisionRawTensor("proto", new float[32 * 160 * 160], new[] { 1, 32, 160, 160 })
                }
            };
            var context = new VisionOutputParserContext(
                new Vector2Int(1280, 720),
                0.25f,
                0.5f,
                new[] { "person" },
                new Vector2Int(640, 640));

            VisionParsedOutput parsed = parser.Parse(raw, context);

            Assert.That(parsed.detections, Has.Length.EqualTo(1));
            Assert.That(parsed.masks, Has.Length.EqualTo(1));
            AssertRect(parsed.detections[0].sourceRect, new Rect(512, 108, 256, 144));
            Assert.That(parsed.diagnostics.modelOutput, Does.Contain("1x116x1"));
        }

        [Test]
        public void YoloSegmentationParser_ReconstructsContourFromPrototype()
        {
            var parser = new YoloSegmentationOutputParser();
            float[] data = new float[300 * 38];
            WriteYolo26Row(data, 38, 0, 100f, 50f, 300f, 250f, 0.82f, 0);
            data[6] = 1f;
            float[] prototype = new float[32 * 16 * 16];
            for (int i = 0; i < 16 * 16; i++)
                prototype[i] = 1f;
            var raw = new VisionRawModelOutput
            {
                tensors = new[]
                {
                    new VisionRawTensor("seg", data, new[] { 1, 300, 38 }),
                    new VisionRawTensor("proto", prototype, new[] { 1, 32, 16, 16 })
                }
            };
            var context = new VisionOutputParserContext(
                new Vector2Int(1280, 720),
                0.25f,
                0.5f,
                new[] { "person" },
                new Vector2Int(640, 640));

            VisionParsedOutput parsed = parser.Parse(raw, context);

            Assert.That(parsed.masks, Has.Length.EqualTo(1));
            Assert.That(parsed.masks[0].HasContour, Is.True);
            Assert.That(parsed.masks[0].normalizedContour.Length, Is.GreaterThanOrEqualTo(4));
        }

        [Test]
        public void YoloSegmentationParser_ParsesYolo26EndToEndSegmentationOutput()
        {
            var parser = new YoloSegmentationOutputParser();
            float[] data = new float[300 * 38];
            WriteYolo26Row(data, 38, 0, 100f, 50f, 300f, 250f, 0.82f, 0);
            var raw = new VisionRawModelOutput
            {
                tensors = new[]
                {
                    new VisionRawTensor("seg", data, new[] { 1, 300, 38 }),
                    new VisionRawTensor("proto", new float[32 * 160 * 160], new[] { 1, 32, 160, 160 })
                }
            };
            var context = new VisionOutputParserContext(
                new Vector2Int(1280, 720),
                0.25f,
                0.5f,
                new[] { "person" },
                new Vector2Int(640, 640));

            VisionParsedOutput parsed = parser.Parse(raw, context);

            Assert.That(parsed.detections, Has.Length.EqualTo(1));
            Assert.That(parsed.masks, Has.Length.EqualTo(1));
            AssertRect(parsed.detections[0].sourceRect, new Rect(200, 56.25f, 400, 225));
            Assert.That(parsed.masks[0].texture, Is.Null);
        }

        [Test]
        public void ParsedOutput_ToFrameResult_PreservesMetadata()
        {
            var parsed = new VisionParsedOutput
            {
                detections = new[]
                {
                    new VisionDetection { classId = 3, confidence = 0.8f }
                }
            };

            VisionFrameResult frame = parsed.ToFrameResult(9, 4.5, new Vector2Int(320, 240));

            Assert.That(frame.frameIndex, Is.EqualTo(9));
            Assert.That(frame.timestamp, Is.EqualTo(4.5));
            Assert.That(frame.sourceSize, Is.EqualTo(new Vector2Int(320, 240)));
            Assert.That(frame.detections.Length, Is.EqualTo(1));
            Assert.That(frame.TotalResultCount, Is.EqualTo(1));
        }

        [Test]
        public void ParserRegistry_ResolvesParserByExplicitId()
        {
            var profile = ScriptableObject.CreateInstance<VisionModelProfile>();
            profile.family = VisionModelFamily.YOLO;
            profile.capabilities = VisionModelCapability.Detection;
            profile.parserId = "yolo.detection.rows";
            var registry = VisionOutputParserRegistry.CreateDefault();

            bool resolved = registry.TryGetParser(profile, out IVisionOutputParser parser);

            Assert.That(resolved, Is.True);
            Assert.That(parser, Is.TypeOf<YoloDetectionOutputParser>());
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void ParserRegistry_ResolvesPoseAndSegmentationParsersByExplicitId()
        {
            var profile = ScriptableObject.CreateInstance<VisionModelProfile>();
            profile.family = VisionModelFamily.YOLO;
            profile.capabilities = VisionModelCapability.Detection | VisionModelCapability.HumanDetection | VisionModelCapability.Pose2D;
            profile.parserId = "yolo.pose2d.rows";
            var registry = VisionOutputParserRegistry.CreateDefault();

            bool poseResolved = registry.TryGetParser(profile, out IVisionOutputParser poseParser);
            profile.capabilities = VisionModelCapability.Detection | VisionModelCapability.Segmentation;
            profile.parserId = "yolo.segmentation.rows";
            bool segmentationResolved = registry.TryGetParser(profile, out IVisionOutputParser segmentationParser);

            Assert.That(poseResolved, Is.True);
            Assert.That(poseParser, Is.TypeOf<YoloPose2DOutputParser>());
            Assert.That(segmentationResolved, Is.True);
            Assert.That(segmentationParser, Is.TypeOf<YoloSegmentationOutputParser>());
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void ParserRegistry_UnknownExplicitId_DoesNotFallbackToCapabilityMatch()
        {
            var profile = ScriptableObject.CreateInstance<VisionModelProfile>();
            profile.family = VisionModelFamily.YOLO;
            profile.capabilities = VisionModelCapability.Detection;
            profile.parserId = "unknown.parser";
            var registry = VisionOutputParserRegistry.CreateDefault();

            bool resolved = registry.TryGetParser(
                profile,
                out IVisionOutputParser parser,
                out string code,
                out string message);

            Assert.That(resolved, Is.False);
            Assert.That(parser, Is.Null);
            Assert.That(code, Is.EqualTo("parser.id.not_registered"));
            Assert.That(message, Does.Contain("unknown.parser"));
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void ParserRegistry_DuplicateParserId_Throws()
        {
            var registry = new VisionOutputParserRegistry();
            registry.Register(new YoloDetectionOutputParser());

            Assert.That(
                () => registry.Register(new YoloDetectionOutputParser()),
                Throws.InvalidOperationException);
        }

        private static void AssertRect(Rect actual, Rect expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.0001f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.0001f));
            Assert.That(actual.width, Is.EqualTo(expected.width).Within(0.0001f));
            Assert.That(actual.height, Is.EqualTo(expected.height).Within(0.0001f));
        }

        private static float[] CreatePoseRow()
        {
            var row = new float[56];
            row[0] = 0.50f;
            row[1] = 0.50f;
            row[2] = 0.20f;
            row[3] = 0.40f;
            row[4] = 0.90f;

            for (int i = 0; i < 17; i++)
            {
                int offset = 5 + i * 3;
                row[offset] = Mathf.Clamp01(0.50f + i * 0.01f);
                row[offset + 1] = Mathf.Clamp01(0.40f + i * 0.01f);
                row[offset + 2] = 0.80f;
            }

            return row;
        }

        private static void WriteChannelFirstBox(
            float[] data,
            int rowCount,
            int row,
            float centerX,
            float centerY,
            float width,
            float height,
            int classId,
            float score)
        {
            data[0 * rowCount + row] = centerX;
            data[1 * rowCount + row] = centerY;
            data[2 * rowCount + row] = width;
            data[3 * rowCount + row] = height;
            data[(4 + classId) * rowCount + row] = score;
        }

        private static void WriteChannelFirstPose(
            float[] data,
            int rowCount,
            int row,
            float centerX,
            float centerY,
            float width,
            float height,
            float score)
        {
            data[0 * rowCount + row] = centerX;
            data[1 * rowCount + row] = centerY;
            data[2 * rowCount + row] = width;
            data[3 * rowCount + row] = height;
            data[4 * rowCount + row] = score;

            for (int i = 0; i < 17; i++)
            {
                int offset = 5 + i * 3;
                data[offset * rowCount + row] = centerX;
                data[(offset + 1) * rowCount + row] = centerY;
                data[(offset + 2) * rowCount + row] = 0.8f;
            }
        }

        private static void WriteYolo26Row(
            float[] data,
            int stride,
            int row,
            float x1,
            float y1,
            float x2,
            float y2,
            float score,
            int classId)
        {
            int offset = row * stride;
            data[offset] = x1;
            data[offset + 1] = y1;
            data[offset + 2] = x2;
            data[offset + 3] = y2;
            data[offset + 4] = score;
            data[offset + 5] = classId;
        }

        private static void WriteYolo26PoseKeypoints(float[] data, int stride, int row, float x, float y, float score)
        {
            int rowOffset = row * stride;
            for (int i = 0; i < 17; i++)
            {
                int offset = rowOffset + 6 + i * 3;
                data[offset] = x;
                data[offset + 1] = y;
                data[offset + 2] = score;
            }
        }
    }
}
