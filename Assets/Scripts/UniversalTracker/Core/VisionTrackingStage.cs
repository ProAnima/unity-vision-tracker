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
            result.stats.trackingMs = 0f;
            result.stats.totalMs = result.stats.preprocessMs + result.stats.inferenceMs + result.stats.postprocessMs + result.stats.trackingMs;
        }

        public void Reset()
        {
            tracker = null;
        }
    }
}
