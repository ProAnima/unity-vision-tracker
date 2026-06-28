using UnityEngine;

namespace UniversalTracker.Core
{
    public sealed class UnityCameraFrameSource : IVisionFrameSource
    {
        private readonly Camera camera;
        private readonly RenderTexture targetTexture;
        private readonly bool ownsTargetTexture;
        private RenderTexture previousTargetTexture;
        private int frameIndex;

        public UnityCameraFrameSource(Camera camera, RenderTexture targetTexture, bool ownsTargetTexture = false)
        {
            this.camera = camera;
            this.targetTexture = targetTexture;
            this.ownsTargetTexture = ownsTargetTexture;
        }

        public bool IsReady => camera != null && targetTexture != null && targetTexture.IsCreated();
        public Vector2Int SourceSize => targetTexture != null ? new Vector2Int(targetTexture.width, targetTexture.height) : Vector2Int.zero;
        public VisionFrameSourceType SourceType => VisionFrameSourceType.UnityCamera;

        public void Initialize()
        {
            if (camera == null)
                return;

            previousTargetTexture = camera.targetTexture;

            if (targetTexture != null && !targetTexture.IsCreated())
                targetTexture.Create();

            camera.targetTexture = targetTexture;
        }

        public bool TryGetFrame(out VisionFrame frame)
        {
            frame = default;
            if (!IsReady)
                return false;

            camera.Render();
            frame = new VisionFrame(
                targetTexture,
                ++frameIndex,
                Time.realtimeSinceStartupAsDouble,
                SourceSize,
                SourceType,
                VisionFrameOrientation.Rotation0,
                false,
                false,
                camera.projectionMatrix,
                camera.cameraToWorldMatrix,
                true);
            return frame.IsValid;
        }

        public void Dispose()
        {
            if (camera != null && camera.targetTexture == targetTexture)
                camera.targetTexture = previousTargetTexture;

            if (ownsTargetTexture && targetTexture != null)
            {
                targetTexture.Release();
                Object.Destroy(targetTexture);
            }
        }
    }
}
