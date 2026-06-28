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

            var detections = new List<VisionDetection>();
            var poses = new List<VisionPose>();
            for (int row = 0; row < rows.rowCount; row++)
            {
                float confidence = Mathf.Clamp01(rows.Get(row, 4));
                if (confidence < context.confidenceThreshold)
                    continue;

                Rect normalized = YoloOutputParserUtility.CenterToNormalizedRect(rows, row, context);

                VisionDetection detection = YoloOutputParserUtility.CreateDetection(
                    0,
                    ResolvePersonLabel(context.labels),
                    confidence,
                    normalized,
                    context.sourceSize);

                detections.Add(detection);
                poses.Add(new VisionPose
                {
                    personId = -1,
                    confidence = confidence,
                    normalizedRect = normalized,
                    sourceRect = detection.sourceRect,
                    keypoints = ParseKeypoints(rows, row, context),
                    skeleton = CocoSkeleton,
                    trackState = VisionTrackState.None
                });
            }

            return new VisionParsedOutput
            {
                detections = detections.ToArray(),
                poses = poses.ToArray(),
                stats = VisionPerformanceStats.FromStages(0f, rawOutput.inferenceMs, 0f, 0f),
                diagnostics = new VisionFrameDiagnostics
                {
                    parserId = ParserId,
                    modelOutput = $"{tensor.name} [{FormatShape(tensor.shape)}], {rows.LayoutLabel}",
                    candidateCount = detections.Count,
                    acceptedCount = detections.Count,
                    maxConfidence = ResolveMaxConfidence(poses)
                }
            };
        }

        private static string ResolvePersonLabel(string[] labels)
        {
            return labels != null && labels.Length > 0 && !string.IsNullOrWhiteSpace(labels[0])
                ? labels[0]
                : "person";
        }

        private static VisionKeypoint[] ParseKeypoints(YoloTensorRows rows, int row, VisionOutputParserContext context)
        {
            var keypoints = new VisionKeypoint[KeypointCount];
            for (int i = 0; i < KeypointCount; i++)
            {
                int keypointOffset = 5 + i * 3;
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

        private static float ResolveMaxConfidence(List<VisionPose> poses)
        {
            float max = 0f;
            for (int i = 0; i < poses.Count; i++)
                max = Mathf.Max(max, poses[i].confidence);

            return max;
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
    }
}
