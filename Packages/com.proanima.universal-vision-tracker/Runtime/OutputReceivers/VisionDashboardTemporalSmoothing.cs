using System.Collections.Generic;
using UnityEngine;
using UniversalTracker.Core;

namespace UniversalTracker.OutputReceivers
{
    internal static class VisionDashboardTemporalSmoothing
    {
        public const int HoldFrames = 2;

        public static int DetectionKey(VisionDetection detection, int index)
        {
            return detection.IsTracked
                ? 100000 + detection.trackId
                : 200000 + detection.classId * 997 + index;
        }

        public static int MaskKey(VisionMask mask, int index)
        {
            return mask.trackId >= 0
                ? 300000 + mask.trackId
                : 400000 + mask.classId * 997 + index;
        }

        public static int PoseKey(VisionPose pose, int poseIndex, int keypointIndex)
        {
            if (pose.personId >= 0)
                return 500000 + pose.personId * 1000 + keypointIndex;

            return 600000 + SpatialPoseKey(pose, poseIndex) * 1000 + keypointIndex;
        }

        public static Rect SmoothRect(Dictionary<int, Rect> cache, int key, Rect current, float smoothing)
        {
            smoothing = Mathf.Clamp01(smoothing);
            if (!cache.TryGetValue(key, out Rect previous) || smoothing <= 0f || ShouldResetRect(previous, current))
            {
                cache[key] = current;
                return current;
            }

            Rect smoothed = new Rect(
                Mathf.Lerp(current.x, previous.x, smoothing),
                Mathf.Lerp(current.y, previous.y, smoothing),
                Mathf.Lerp(current.width, previous.width, smoothing),
                Mathf.Lerp(current.height, previous.height, smoothing));
            cache[key] = smoothed;
            return smoothed;
        }

        public static Vector2 SmoothPoint(Dictionary<int, Vector2> cache, int key, Vector2 current, float smoothing)
        {
            return SmoothPoint(cache, key, current, smoothing, float.PositiveInfinity);
        }

        public static Vector2 SmoothPoint(Dictionary<int, Vector2> cache, int key, Vector2 current, float smoothing, float resetDistance)
        {
            smoothing = Mathf.Clamp01(smoothing);
            if (!cache.TryGetValue(key, out Vector2 previous) || smoothing <= 0f || ShouldResetPoint(previous, current, resetDistance))
            {
                cache[key] = current;
                return current;
            }

            Vector2 smoothed = Vector2.Lerp(current, previous, smoothing);
            cache[key] = smoothed;
            return smoothed;
        }

        public static Vector2[] SmoothPoints(Dictionary<int, Vector2[]> cache, int key, Vector2[] current, float smoothing)
        {
            if (current == null || current.Length == 0)
            {
                cache.Remove(key);
                return current;
            }

            smoothing = Mathf.Clamp01(smoothing);
            if (!cache.TryGetValue(key, out Vector2[] previous) || previous.Length != current.Length || smoothing <= 0f)
            {
                Vector2[] copy = new Vector2[current.Length];
                System.Array.Copy(current, copy, current.Length);
                cache[key] = copy;
                return copy;
            }

            for (int i = 0; i < previous.Length; i++)
                previous[i] = Vector2.Lerp(current[i], previous[i], smoothing);

            return previous;
        }

        public static float HeldOpacity(int age)
        {
            float t = Mathf.Clamp01(age / (float)(HoldFrames + 1));
            return Mathf.Lerp(0.5f, 0.2f, t);
        }

        private static bool ShouldResetRect(Rect previous, Rect current)
        {
            float centerDistance = Vector2.Distance(previous.center, current.center);
            if (centerDistance > 0.35f)
                return true;

            float previousArea = Mathf.Max(0.0001f, previous.width * previous.height);
            float currentArea = Mathf.Max(0.0001f, current.width * current.height);
            float ratio = Mathf.Max(previousArea, currentArea) / Mathf.Min(previousArea, currentArea);
            return ratio > 4f;
        }

        private static bool ShouldResetPoint(Vector2 previous, Vector2 current, float resetDistance)
        {
            return resetDistance > 0f &&
                   !float.IsInfinity(resetDistance) &&
                   Vector2.Distance(previous, current) > resetDistance;
        }

        private static int SpatialPoseKey(VisionPose pose, int poseIndex)
        {
            Rect rect = pose.normalizedRect;
            if (rect.width <= 0f || rect.height <= 0f)
                return Mathf.Max(0, poseIndex);

            int centerX = Mathf.Clamp(Mathf.RoundToInt(rect.center.x * 32f), 0, 32);
            int centerY = Mathf.Clamp(Mathf.RoundToInt(rect.center.y * 32f), 0, 32);
            int size = Mathf.Clamp(Mathf.RoundToInt((rect.width + rect.height) * 16f), 0, 64);
            return (centerY * 33 + centerX) * 65 + size;
        }
    }
}
