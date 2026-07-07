using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UniversalTracker.Core;

namespace UniversalTracker.OutputReceivers
{
    internal static class VisionDashboardElementPool
    {
        public static VisualElement GetElement(List<VisualElement> pool, VisualElement parent, Func<VisualElement> factory, int index)
        {
            while (pool.Count <= index)
            {
                VisualElement element = factory();
                parent.Add(element);
                pool.Add(element);
            }

            pool[index].style.display = DisplayStyle.Flex;
            return pool[index];
        }

        public static Label GetLabel(List<Label> pool, VisualElement parent, int index)
        {
            while (pool.Count <= index)
            {
                Label label = VisionToolkitDashboardPrimitives.CreateOverlayLabel();
                parent.Add(label);
                pool.Add(label);
            }

            pool[index].style.display = DisplayStyle.Flex;
            return pool[index];
        }

        public static void SetPoolActive(List<VisualElement> pool, int activeCount)
        {
            for (int i = 0; i < pool.Count; i++)
                pool[i].style.display = i < activeCount ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public static void SetLabelsActive(List<Label> pool, int activeCount)
        {
            for (int i = 0; i < pool.Count; i++)
                pool[i].style.display = i < activeCount ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    internal sealed class VisionDashboardOverlayState
    {
        public VisualElement maskLayer;
        public VisualElement detectionLayer;
        public VisualElement boneLayer;
        public VisualElement keypointLayer;
        public VisualElement labelLayer;
        public readonly List<VisualElement> masks = new List<VisualElement>();
        public readonly List<VisualElement> detections = new List<VisualElement>();
        public readonly List<Label> labels = new List<Label>();
        public readonly List<VisualElement> keypoints = new List<VisualElement>();
        public readonly List<VisualElement> bones = new List<VisualElement>();
        public readonly List<VisualElement> maskContourSegments = new List<VisualElement>();
        public readonly HashSet<int> activeDetectionKeys = new HashSet<int>();
        public readonly HashSet<int> activeMaskKeys = new HashSet<int>();
        public readonly HashSet<int> activeKeypointKeys = new HashSet<int>();
        public readonly List<int> scratchKeys = new List<int>();
        public readonly Dictionary<int, Rect> smoothedDetectionRects = new Dictionary<int, Rect>();
        public readonly Dictionary<int, Rect> smoothedMaskRects = new Dictionary<int, Rect>();
        public readonly Dictionary<int, VisionDetection> detectionSnapshots = new Dictionary<int, VisionDetection>();
        public readonly Dictionary<int, VisionMask> maskSnapshots = new Dictionary<int, VisionMask>();
        public readonly Dictionary<int, int> detectionLastSeen = new Dictionary<int, int>();
        public readonly Dictionary<int, int> maskLastSeen = new Dictionary<int, int>();
        public readonly Dictionary<int, int> keypointLastSeen = new Dictionary<int, int>();
        public readonly Dictionary<int, Vector2> smoothedKeypoints = new Dictionary<int, Vector2>();
        public readonly Dictionary<int, Vector2[]> smoothedMaskContours = new Dictionary<int, Vector2[]>();
        public readonly Dictionary<int, Vector2[]> smoothedMaskContourSegments = new Dictionary<int, Vector2[]>();
        public Vector2[] scratchPosePoints = Array.Empty<Vector2>();
        public bool[] scratchPoseVisible = Array.Empty<bool>();
        public int renderSequence;

        public void ClearTemporal()
        {
            ClearDetectionTemporal();
            ClearMaskTemporal();
            ClearPoseTemporal();
            renderSequence = 0;
        }

        public void ClearDetectionTemporal()
        {
            activeDetectionKeys.Clear();
            smoothedDetectionRects.Clear();
            detectionSnapshots.Clear();
            detectionLastSeen.Clear();
        }

        public void ClearMaskTemporal()
        {
            activeMaskKeys.Clear();
            smoothedMaskRects.Clear();
            smoothedMaskContours.Clear();
            smoothedMaskContourSegments.Clear();
            maskSnapshots.Clear();
            maskLastSeen.Clear();
        }

        public void ClearPoseTemporal()
        {
            activeKeypointKeys.Clear();
            smoothedKeypoints.Clear();
            keypointLastSeen.Clear();
        }
    }
}
