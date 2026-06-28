using UnityEngine;

namespace UniversalTracker.Core
{
    public sealed class WebCamFrameSource : IVisionFrameSource
    {
        private readonly string deviceName;
        private readonly int requestedWidth;
        private readonly int requestedHeight;
        private readonly int requestedFps;
        private WebCamTexture texture;
        private int frameIndex;

        public WebCamFrameSource(string deviceName = null, int requestedWidth = 1280, int requestedHeight = 720, int requestedFps = 30)
        {
            this.deviceName = deviceName;
            this.requestedWidth = requestedWidth;
            this.requestedHeight = requestedHeight;
            this.requestedFps = requestedFps;
        }

        public bool IsReady => texture != null && texture.isPlaying && texture.width > 16 && texture.height > 16;
        public Vector2Int SourceSize => IsReady ? new Vector2Int(texture.width, texture.height) : Vector2Int.zero;
        public VisionFrameSourceType SourceType => VisionFrameSourceType.WebCam;

        public void Initialize()
        {
            texture = string.IsNullOrWhiteSpace(deviceName)
                ? new WebCamTexture(requestedWidth, requestedHeight, requestedFps)
                : new WebCamTexture(deviceName, requestedWidth, requestedHeight, requestedFps);
            texture.Play();
        }

        public bool TryGetFrame(out VisionFrame frame)
        {
            frame = default;
            if (!IsReady)
                return false;

            frame = new VisionFrame(
                texture,
                ++frameIndex,
                Time.realtimeSinceStartupAsDouble,
                SourceSize,
                SourceType,
                ToOrientation(texture.videoRotationAngle),
                texture.videoVerticallyMirrored);
            return frame.IsValid;
        }

        public void Dispose()
        {
            if (texture == null)
                return;

            texture.Stop();
            Object.Destroy(texture);
            texture = null;
        }

        private static VisionFrameOrientation ToOrientation(int rotationAngle)
        {
            return rotationAngle switch
            {
                90 => VisionFrameOrientation.Rotation90,
                180 => VisionFrameOrientation.Rotation180,
                270 => VisionFrameOrientation.Rotation270,
                _ => VisionFrameOrientation.Rotation0
            };
        }
    }
}
