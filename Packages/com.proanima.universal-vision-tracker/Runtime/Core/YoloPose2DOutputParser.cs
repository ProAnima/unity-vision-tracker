using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniversalTracker.Core
{
    public sealed class YoloPose2DOutputParser : IVisionOutputParser
    {
        private const int KeypointCount = 17;

        public string ParserId => "yolo.pose2d.rows";
        public VisionModelCapability Capabilities => VisionModelCapability.Detection | VisionModelCapability.HumanDetection | VisionModelCapability.Pose2D;

        public bool CanParse(VisionModelProfile profile)
        {
            return profile != null &&
                   profile.family == VisionModelFamily.YOLO &&
                   profile.Supports(VisionModelCapability.Pose2D);
        }

        public VisionParsedOutput Parse(VisionRawModelOutput rawOutput, VisionOutputParserContext context)
        {
            if (rawOutput == null || !rawOutput.HasTensors || !rawOutput.tensors[0].IsValid)
                return VisionParsedOutput.Empty;

            VisionRawTensor tensor = rawOutput.tensors[0];
            if (!YoloOutputParserUtility.TryResolveRows(tensor, out YoloTensorRows rows) || rows.stride < 5 + KeypointCount * 3)
                return VisionParsedOutput.Empty;

            bool isEndToEnd = YoloOutputParserUtility.IsEndToEndRows(rows, 57);
            var candidates = new List<PoseCandidate>();
            float maxConfidence = 0f;
            for (int row = 0; row < rows.rowCount; row++)
            {
                float confidence = Mathf.Clamp01(rows.Get(row, 4));
                maxConfidence = Mathf.Max(maxConfidence, confidence);
                if (confidence < context.confidenceThreshold)
                    continue;

                Rect normalized = isEndToEnd
                    ? YoloOutputParserUtility.CornersToNormalizedRect(rows, row, context)
                    : YoloOutputParserUtility.CenterToNormalizedRect(rows, row, context);
                int classId = isEndToEnd ? Mathf.Max(0, Mathf.RoundToInt(rows.Get(row, 5))) : 0;

                VisionDetection detection = YoloOutputParserUtility.CreateDetection(
                    classId,
                    ResolvePersonLabel(classId, context.labels),
                    confidence,
                    normalized,
                    context.sourceSize);

                candidates.Add(new PoseCandidate(
                    detection,
                    new VisionPose
                    {
                        personId = -1,
                        confidence = confidence,
                        normalizedRect = normalized,
                        sourceRect = detection.sourceRect,
                        keypoints = ParseKeypoints(rows, row, context, isEndToEnd ? 6 : 5),
                        skeleton = CocoSkeleton,
                        trackState = VisionTrackState.None
                    }));
            }

            ApplyNms(candidates, context.nmsThreshold, out VisionDetection[] detections, out VisionPose[] poses);
            return new VisionParsedOutput
            {
                detections = detections,
                poses = poses,
                stats = VisionPerformanceStats.FromStages(0f, rawOutput.inferenceMs, 0f, 0f),
                diagnostics = new VisionFrameDiagnostics
                {
                    parserId = ParserId,
                    modelOutput = $"{tensor.name} [{FormatShape(tensor.shape)}], {rows.LayoutLabel}",
                    candidateCount = candidates.Count,
                    acceptedCount = detections.Length,
                    maxConfidence = maxConfidence
                }
            };
        }

        private static string ResolvePersonLabel(int classId, string[] labels)
        {
            return labels != null && classId >= 0 && classId < labels.Length && !string.IsNullOrWhiteSpace(labels[classId])
                ? labels[classId]
                : "person";
        }

        private static VisionKeypoint[] ParseKeypoints(YoloTensorRows rows, int row, VisionOutputParserContext context, int offset)
        {
            var keypoints = new VisionKeypoint[KeypointCount];
            for (int i = 0; i < KeypointCount; i++)
            {
                int keypointOffset = offset + i * 3;
                Vector2 normalized = YoloOutputParserUtility.ReadNormalizedPoint(rows, row, keypointOffset, keypointOffset + 1, context);
                float confidence = Mathf.Clamp01(rows.Get(row, keypointOffset + 2));
                keypoints[i] = new VisionKeypoint
                {
                    index = i,
                    name = CocoKeypointNames[i],
                    normalizedPosition = normalized,
                    sourcePosition = YoloOutputParserUtility.NormalizedToSourcePoint(normalized, context.sourceSize),
                    confidence = confidence,
                    isVisible = confidence > 0.01f
                };
            }

            return keypoints;
        }

        private static void ApplyNms(List<PoseCandidate> candidates, float threshold, out VisionDetection[] detections, out VisionPose[] poses)
        {
            if (candidates.Count == 0)
            {
                detections = Array.Empty<VisionDetection>();
                poses = Array.Empty<VisionPose>();
                return;
            }

            candidates.Sort((a, b) => b.detection.confidence.CompareTo(a.detection.confidence));
            var keptDetections = new List<VisionDetection>();
            var keptPoses = new List<VisionPose>();
            var suppressed = new bool[candidates.Count];

            for (int i = 0; i < candidates.Count; i++)
            {
                if (suppressed[i])
                    continue;

                PoseCandidate current = candidates[i];
                keptDetections.Add(current.detection);
                keptPoses.Add(current.pose);

                for (int j = i + 1; j < candidates.Count; j++)
                {
                    if (suppressed[j])
                        continue;

                    if (YoloOutputParserUtility.CalculateIoU(current.detection.normalizedRect, candidates[j].detection.normalizedRect) > threshold)
                        suppressed[j] = true;
                }
            }

            detections = keptDetections.ToArray();
            poses = keptPoses.ToArray();
        }

        private static string FormatShape(int[] shape)
        {
            if (shape == null || shape.Length == 0)
                return "-";

            return string.Join("x", shape);
        }

        private static readonly string[] CocoKeypointNames =
        {
            "nose", "left_eye", "right_eye", "left_ear", "right_ear",
            "left_shoulder", "right_shoulder", "left_elbow", "right_elbow",
            "left_wrist", "right_wrist", "left_hip", "right_hip",
            "left_knee", "right_knee", "left_ankle", "right_ankle"
        };

        private static readonly VisionSkeletonDefinition CocoSkeleton = new VisionSkeletonDefinition
        {
            name = "COCO-17",
            bones = new[]
            {
                Bone(5, 7, "left_upper_arm"),
                Bone(7, 9, "left_lower_arm"),
                Bone(6, 8, "right_upper_arm"),
                Bone(8, 10, "right_lower_arm"),
                Bone(5, 6, "shoulders"),
                Bone(5, 11, "left_torso"),
                Bone(6, 12, "right_torso"),
                Bone(11, 12, "hips"),
                Bone(11, 13, "left_upper_leg"),
                Bone(13, 15, "left_lower_leg"),
                Bone(12, 14, "right_upper_leg"),
                Bone(14, 16, "right_lower_leg")
            }
        };

        private static VisionSkeletonBone Bone(int from, int to, string name)
        {
            return new VisionSkeletonBone { from = from, to = to, name = name };
        }

        private readonly struct PoseCandidate
        {
            public readonly VisionDetection detection;
            public readonly VisionPose pose;

            public PoseCandidate(VisionDetection detection, VisionPose pose)
            {
                this.detection = detection;
                this.pose = pose;
            }
        }
    }
}
