using System;
using UnityEngine;

namespace UniversalTracker.Core
{
    /// <summary>
    /// Canonical public output for one processed frame.
    /// </summary>
    [Serializable]
    public sealed class VisionFrameResult
    {
        public int frameIndex;
        public double timestamp;
        public Vector2Int sourceSize;
        public VisionDetection[] detections = Array.Empty<VisionDetection>();
        public VisionPose[] poses = Array.Empty<VisionPose>();
        public VisionMask[] masks = Array.Empty<VisionMask>();
        public VisionClassification[] classifications = Array.Empty<VisionClassification>();
        public VisionPerformanceStats stats;
        public VisionFrameDiagnostics diagnostics;

        [NonSerialized]
        public Texture sourceTexture;

        public int TotalResultCount =>
            (detections?.Length ?? 0) +
            (poses?.Length ?? 0) +
            (masks?.Length ?? 0) +
            (classifications?.Length ?? 0);

        public bool HasAnyResult => TotalResultCount > 0;

        public static VisionFrameResult Empty(int frameIndex, double timestamp, Vector2Int sourceSize) =>
            new VisionFrameResult
            {
                frameIndex = frameIndex,
                timestamp = timestamp,
                sourceSize = sourceSize
            };
    }

    [Serializable]
    public struct VisionFrameDiagnostics
    {
        public string parserId;
        public string modelOutput;
        public int candidateCount;
        public int acceptedCount;
        public float maxConfidence;
        public string message;

        public bool HasModelOutput => !string.IsNullOrWhiteSpace(modelOutput);
    }

    [Serializable]
    public struct VisionDetection
    {
        public int trackId;
        public int classId;
        public string label;
        public float confidence;
        public Rect sourceRect;
        public Rect normalizedRect;
        public Vector2 sourceCenter;
        public VisionTrackState trackState;

        public bool IsTracked => trackId >= 0 && trackState != VisionTrackState.None;
    }

    [Serializable]
    public struct VisionPose
    {
        public int personId;
        public float confidence;
        public Rect sourceRect;
        public Rect normalizedRect;
        public VisionKeypoint[] keypoints;
        public VisionSkeletonDefinition skeleton;
        public VisionTrackState trackState;

        public int VisibleKeypointCount
        {
            get
            {
                if (keypoints == null)
                    return 0;

                int count = 0;
                for (int i = 0; i < keypoints.Length; i++)
                {
                    if (keypoints[i].isVisible)
                        count++;
                }

                return count;
            }
        }
    }

    [Serializable]
    public struct VisionKeypoint
    {
        public int index;
        public string name;
        public Vector2 sourcePosition;
        public Vector2 normalizedPosition;
        public float confidence;
        public bool isVisible;
    }

    [Serializable]
    public struct VisionSkeletonDefinition
    {
        public string name;
        public VisionSkeletonBone[] bones;
    }

    [Serializable]
    public struct VisionSkeletonBone
    {
        public int from;
        public int to;
        public string name;
    }

    [Serializable]
    public struct VisionMask
    {
        public int trackId;
        public int classId;
        public string label;
        public float confidence;
        public Rect sourceRect;
        public Rect normalizedRect;
        public Texture2D texture;
    }

    [Serializable]
    public struct VisionClassification
    {
        public int classId;
        public string label;
        public float confidence;
    }

    [Serializable]
    public struct VisionPerformanceStats
    {
        public float preprocessMs;
        public float inferenceMs;
        public float postprocessMs;
        public float trackingMs;
        public float totalMs;

        public static VisionPerformanceStats FromStages(float preprocessMs, float inferenceMs, float postprocessMs, float trackingMs)
        {
            return new VisionPerformanceStats
            {
                preprocessMs = preprocessMs,
                inferenceMs = inferenceMs,
                postprocessMs = postprocessMs,
                trackingMs = trackingMs,
                totalMs = preprocessMs + inferenceMs + postprocessMs + trackingMs
            };
        }
    }
}
