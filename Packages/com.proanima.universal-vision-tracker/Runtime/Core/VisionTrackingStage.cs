using System;
using UnityEngine;
using UniversalTracker.Tracking;

namespace UniversalTracker.Core
{
    internal sealed class VisionTrackingStage
    {
        private ITracker tracker;

        public TrackedObject[] TrackedObjects => tracker != null
            ? tracker.GetAllTrackedObjects()
            : Array.Empty<TrackedObject>();

        public void Configure(bool enabled, TrackerType trackerType, float iouThreshold, int maxMissedFrames)
        {
            if (!enabled)
            {
                tracker = null;
                return;
            }

            tracker = trackerType switch
            {
                TrackerType.IOU => new IOUTracker(iouThreshold, maxMissedFrames),
                TrackerType.SORT => new SORTTracker(iouThreshold, maxMissedFrames),
                _ => null
            };
        }

        public void Apply(VisionFrameResult result, float deltaTime)
        {
            if (result == null || tracker == null || result.detections == null || result.detections.Length == 0)
                return;

            TrackedObject[] trackedObjects = tracker.Update(result.detections, deltaTime);
            var trackedDetections = new VisionDetection[trackedObjects.Length];
            for (int i = 0; i < trackedObjects.Length; i++)
                trackedDetections[i] = trackedObjects[i].currentDetection;

            result.detections = trackedDetections;
            ApplyPoseTrackIds(result.poses, trackedDetections);
            result.stats.trackingMs = 0f;
            result.stats.totalMs = result.stats.preprocessMs + result.stats.inferenceMs + result.stats.postprocessMs + result.stats.trackingMs;
        }

        public void Reset()
        {
            tracker = null;
        }

        private static void ApplyPoseTrackIds(VisionPose[] poses, VisionDetection[] trackedDetections)
        {
            if (poses == null || poses.Length == 0 || trackedDetections == null || trackedDetections.Length == 0)
                return;

            var usedDetections = new bool[trackedDetections.Length];
            for (int i = 0; i < poses.Length; i++)
            {
                int bestIndex = FindBestTrackedDetection(poses[i], trackedDetections, usedDetections);
                if (bestIndex < 0)
                    continue;

                VisionDetection detection = trackedDetections[bestIndex];
                poses[i].personId = detection.trackId;
                poses[i].trackState = detection.trackState;
                usedDetections[bestIndex] = true;
            }
        }

        private static int FindBestTrackedDetection(VisionPose pose, VisionDetection[] trackedDetections, bool[] usedDetections)
        {
            const float minPoseDetectionIoU = 0.25f;
            int bestIndex = -1;
            float bestIoU = 0f;
            Rect poseRect = GetComparisonRect(pose);
            if (poseRect.width <= 0f || poseRect.height <= 0f)
                return -1;

            for (int i = 0; i < trackedDetections.Length; i++)
            {
                if (usedDetections[i] || !trackedDetections[i].IsTracked)
                    continue;

                float iou = CalculateIoU(poseRect, GetComparisonRect(trackedDetections[i]));
                if (iou <= bestIoU)
                    continue;

                bestIoU = iou;
                bestIndex = i;
            }

            return bestIoU >= minPoseDetectionIoU ? bestIndex : -1;
        }

        private static Rect GetComparisonRect(VisionPose pose)
        {
            return pose.normalizedRect.width > 0f && pose.normalizedRect.height > 0f
                ? pose.normalizedRect
                : pose.sourceRect;
        }

        private static Rect GetComparisonRect(VisionDetection detection)
        {
            return detection.normalizedRect.width > 0f && detection.normalizedRect.height > 0f
                ? detection.normalizedRect
                : detection.sourceRect;
        }

        private static float CalculateIoU(Rect a, Rect b)
        {
            float x1 = Mathf.Max(a.xMin, b.xMin);
            float y1 = Mathf.Max(a.yMin, b.yMin);
            float x2 = Mathf.Min(a.xMax, b.xMax);
            float y2 = Mathf.Min(a.yMax, b.yMax);
            if (x2 <= x1 || y2 <= y1)
                return 0f;

            float intersection = (x2 - x1) * (y2 - y1);
            float areaA = Mathf.Max(0f, a.width) * Mathf.Max(0f, a.height);
            float areaB = Mathf.Max(0f, b.width) * Mathf.Max(0f, b.height);
            float union = areaA + areaB - intersection;
            return union > 0f ? intersection / union : 0f;
        }
    }
}
