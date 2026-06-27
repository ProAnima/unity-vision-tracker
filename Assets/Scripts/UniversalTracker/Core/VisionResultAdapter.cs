using System;
using UnityEngine;

namespace UniversalTracker.Core
{
    /// <summary>
    /// Compatibility bridge from the prototype InferenceResult contract to the production VisionFrameResult contract.
    /// This keeps the current runtime working while receivers and model parsers migrate incrementally.
    /// </summary>
    public static class VisionResultAdapter
    {
        public static VisionFrameResult FromInferenceResult(
            InferenceResult result,
            Texture sourceTexture,
            int frameIndex = 0,
            double timestamp = 0)
        {
            var sourceSize = GetSourceSize(sourceTexture);
            return FromInferenceResult(result, sourceSize, frameIndex, timestamp);
        }

        public static VisionFrameResult FromInferenceResult(
            InferenceResult result,
            Vector2Int sourceSize,
            int frameIndex = 0,
            double timestamp = 0)
        {
            if (result == null)
                return VisionFrameResult.Empty(frameIndex, timestamp, sourceSize);

            var frame = VisionFrameResult.Empty(frameIndex, timestamp, sourceSize);
            frame.detections = ConvertDetections(result.boundingBoxes, sourceSize);
            frame.poses = ConvertPoses(result.keypoints, result.boundingBoxes, sourceSize);
            frame.masks = ConvertMasks(result.masks, sourceSize);
            frame.classifications = ConvertClassifications(result.classifications);
            frame.stats = VisionPerformanceStats.FromStages(0f, result.inferenceTime, 0f, 0f);
            return frame;
        }

        public static VisionDetection[] ConvertDetections(BBoxData[] boxes, Vector2Int sourceSize)
        {
            if (boxes == null || boxes.Length == 0)
                return Array.Empty<VisionDetection>();

            var detections = new VisionDetection[boxes.Length];
            for (int i = 0; i < boxes.Length; i++)
                detections[i] = ConvertDetection(boxes[i], sourceSize);

            return detections;
        }

        public static VisionDetection ConvertDetection(BBoxData box, Vector2Int sourceSize)
        {
            if (box == null)
                return default;

            var normalized = Clamp01Rect(box.rect);
            var sourceRect = NormalizedToSourceRect(normalized, sourceSize);
            int trackId = box.id >= 0 ? box.id : -1;

            return new VisionDetection
            {
                trackId = trackId,
                classId = box.classId,
                label = box.className,
                confidence = box.confidence,
                normalizedRect = normalized,
                sourceRect = sourceRect,
                sourceCenter = sourceRect.center,
                trackState = trackId >= 0 ? VisionTrackState.Tracking : VisionTrackState.None
            };
        }

        public static VisionPose[] ConvertPoses(KeypointData[] keypoints, BBoxData[] boxes, Vector2Int sourceSize)
        {
            if (keypoints == null || keypoints.Length == 0)
                return Array.Empty<VisionPose>();

            var poses = new VisionPose[keypoints.Length];
            for (int i = 0; i < keypoints.Length; i++)
            {
                BBoxData box = boxes != null && i < boxes.Length ? boxes[i] : null;
                poses[i] = ConvertPose(keypoints[i], box, sourceSize);
            }

            return poses;
        }

        public static VisionPose ConvertPose(KeypointData keypointData, BBoxData box, Vector2Int sourceSize)
        {
            if (keypointData == null)
                return default;

            var keypoints = ConvertKeypoints(keypointData, sourceSize);
            var normalizedRect = box != null ? Clamp01Rect(box.rect) : EstimateNormalizedBounds(keypoints);
            var sourceRect = NormalizedToSourceRect(normalizedRect, sourceSize);
            int personId = keypointData.personId >= 0 ? keypointData.personId : box?.id ?? -1;

            return new VisionPose
            {
                personId = personId,
                confidence = AverageVisibleConfidence(keypoints),
                normalizedRect = normalizedRect,
                sourceRect = sourceRect,
                keypoints = keypoints,
                skeleton = CreateSkeleton(keypointData.connections),
                trackState = personId >= 0 ? VisionTrackState.Tracking : VisionTrackState.None
            };
        }

        public static VisionMask[] ConvertMasks(MaskData[] masks, Vector2Int sourceSize)
        {
            if (masks == null || masks.Length == 0)
                return Array.Empty<VisionMask>();

            var converted = new VisionMask[masks.Length];
            for (int i = 0; i < masks.Length; i++)
            {
                var mask = masks[i];
                if (mask == null)
                    continue;

                var normalized = Clamp01Rect(mask.bbox);
                converted[i] = new VisionMask
                {
                    trackId = mask.objectId,
                    classId = mask.classId,
                    label = mask.className,
                    confidence = mask.confidence,
                    normalizedRect = normalized,
                    sourceRect = NormalizedToSourceRect(normalized, sourceSize),
                    texture = mask.mask
                };
            }

            return converted;
        }

        public static VisionClassification[] ConvertClassifications(ClassificationData[] classifications)
        {
            if (classifications == null || classifications.Length == 0)
                return Array.Empty<VisionClassification>();

            var converted = new VisionClassification[classifications.Length];
            for (int i = 0; i < classifications.Length; i++)
            {
                var classification = classifications[i];
                if (classification == null)
                    continue;

                converted[i] = new VisionClassification
                {
                    classId = classification.classId,
                    label = classification.className,
                    confidence = classification.confidence
                };
            }

            return converted;
        }

        private static VisionKeypoint[] ConvertKeypoints(KeypointData keypointData, Vector2Int sourceSize)
        {
            if (keypointData.points == null || keypointData.points.Length == 0)
                return Array.Empty<VisionKeypoint>();

            var keypoints = new VisionKeypoint[keypointData.points.Length];
            for (int i = 0; i < keypointData.points.Length; i++)
            {
                float confidence = keypointData.confidences != null && i < keypointData.confidences.Length
                    ? keypointData.confidences[i]
                    : 1f;

                var normalized = Clamp01Point(keypointData.points[i]);
                keypoints[i] = new VisionKeypoint
                {
                    index = i,
                    name = GetCocoKeypointName(i),
                    normalizedPosition = normalized,
                    sourcePosition = NormalizedToSourcePoint(normalized, sourceSize),
                    confidence = confidence,
                    isVisible = confidence > 0f
                };
            }

            return keypoints;
        }

        private static VisionSkeletonDefinition CreateSkeleton(int[] connections)
        {
            if (connections == null || connections.Length < 2)
                return default;

            int boneCount = connections.Length / 2;
            var bones = new VisionSkeletonBone[boneCount];
            for (int i = 0; i < boneCount; i++)
            {
                int from = connections[i * 2];
                int to = connections[i * 2 + 1];
                bones[i] = new VisionSkeletonBone
                {
                    from = from,
                    to = to,
                    name = $"{GetCocoKeypointName(from)}-{GetCocoKeypointName(to)}"
                };
            }

            return new VisionSkeletonDefinition
            {
                name = "COCO-17",
                bones = bones
            };
        }

        private static Rect EstimateNormalizedBounds(VisionKeypoint[] keypoints)
        {
            if (keypoints == null || keypoints.Length == 0)
                return default;

            bool hasAny = false;
            float minX = 1f;
            float minY = 1f;
            float maxX = 0f;
            float maxY = 0f;

            for (int i = 0; i < keypoints.Length; i++)
            {
                if (!keypoints[i].isVisible)
                    continue;

                var point = keypoints[i].normalizedPosition;
                minX = Mathf.Min(minX, point.x);
                minY = Mathf.Min(minY, point.y);
                maxX = Mathf.Max(maxX, point.x);
                maxY = Mathf.Max(maxY, point.y);
                hasAny = true;
            }

            return hasAny ? Rect.MinMaxRect(minX, minY, maxX, maxY) : default;
        }

        private static float AverageVisibleConfidence(VisionKeypoint[] keypoints)
        {
            if (keypoints == null || keypoints.Length == 0)
                return 0f;

            float sum = 0f;
            int count = 0;
            for (int i = 0; i < keypoints.Length; i++)
            {
                if (!keypoints[i].isVisible)
                    continue;

                sum += keypoints[i].confidence;
                count++;
            }

            return count > 0 ? sum / count : 0f;
        }

        private static Vector2Int GetSourceSize(Texture sourceTexture)
        {
            if (sourceTexture == null)
                return Vector2Int.zero;

            return new Vector2Int(sourceTexture.width, sourceTexture.height);
        }

        private static Rect NormalizedToSourceRect(Rect normalized, Vector2Int sourceSize)
        {
            if (sourceSize.x <= 0 || sourceSize.y <= 0)
                return default;

            return new Rect(
                normalized.x * sourceSize.x,
                normalized.y * sourceSize.y,
                normalized.width * sourceSize.x,
                normalized.height * sourceSize.y);
        }

        private static Vector2 NormalizedToSourcePoint(Vector2 normalized, Vector2Int sourceSize)
        {
            if (sourceSize.x <= 0 || sourceSize.y <= 0)
                return Vector2.zero;

            return new Vector2(normalized.x * sourceSize.x, normalized.y * sourceSize.y);
        }

        private static Rect Clamp01Rect(Rect rect)
        {
            float xMin = Mathf.Clamp01(rect.xMin);
            float yMin = Mathf.Clamp01(rect.yMin);
            float xMax = Mathf.Clamp01(rect.xMax);
            float yMax = Mathf.Clamp01(rect.yMax);
            return Rect.MinMaxRect(
                Mathf.Min(xMin, xMax),
                Mathf.Min(yMin, yMax),
                Mathf.Max(xMin, xMax),
                Mathf.Max(yMin, yMax));
        }

        private static Vector2 Clamp01Point(Vector2 point) =>
            new Vector2(Mathf.Clamp01(point.x), Mathf.Clamp01(point.y));

        private static string GetCocoKeypointName(int index)
        {
            return index switch
            {
                0 => "nose",
                1 => "left_eye",
                2 => "right_eye",
                3 => "left_ear",
                4 => "right_ear",
                5 => "left_shoulder",
                6 => "right_shoulder",
                7 => "left_elbow",
                8 => "right_elbow",
                9 => "left_wrist",
                10 => "right_wrist",
                11 => "left_hip",
                12 => "right_hip",
                13 => "left_knee",
                14 => "right_knee",
                15 => "left_ankle",
                16 => "right_ankle",
                _ => $"keypoint_{index}"
            };
        }
    }
}

