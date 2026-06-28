using System;
using UnityEngine;

namespace UniversalTracker.Core
{
    /// <summary>
    /// Immutable frame package emitted by any frame source before preprocessing.
    /// </summary>
    public readonly struct VisionFrame
    {
        public readonly Texture texture;
        public readonly int frameIndex;
        public readonly double timestamp;
        public readonly Vector2Int sourceSize;
        public readonly VisionFrameSourceType sourceType;
        public readonly VisionFrameOrientation orientation;
        public readonly bool mirroredX;
        public readonly bool mirroredY;
        public readonly Matrix4x4 cameraProjection;
        public readonly Matrix4x4 cameraToWorld;
        public readonly bool hasCameraMatrices;

        public VisionFrame(
            Texture texture,
            int frameIndex,
            double timestamp,
            Vector2Int sourceSize,
            VisionFrameSourceType sourceType = VisionFrameSourceType.Unknown,
            VisionFrameOrientation orientation = VisionFrameOrientation.Rotation0,
            bool mirroredX = false,
            bool mirroredY = false,
            Matrix4x4 cameraProjection = default,
            Matrix4x4 cameraToWorld = default,
            bool hasCameraMatrices = false)
        {
            this.texture = texture;
            this.frameIndex = frameIndex;
            this.timestamp = timestamp;
            this.sourceSize = sourceSize;
            this.sourceType = sourceType;
            this.orientation = orientation;
            this.mirroredX = mirroredX;
            this.mirroredY = mirroredY;
            this.cameraProjection = cameraProjection;
            this.cameraToWorld = cameraToWorld;
            this.hasCameraMatrices = hasCameraMatrices;
        }

        public bool IsValid => texture != null && sourceSize.x > 0 && sourceSize.y > 0;

        public static VisionFrame FromTexture(Texture texture, int frameIndex = 0, double timestamp = 0)
        {
            if (texture == null)
                return default;

            return new VisionFrame(
                texture,
                frameIndex,
                timestamp,
                new Vector2Int(texture.width, texture.height),
                VisionFrameSourceType.Texture);
        }
    }
}

