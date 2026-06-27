using System;
using UnityEngine;
using UnityEngine.Video;

namespace UniversalTracker.Core
{
    public sealed class TextureFrameSource : IVisionFrameSource
    {
        private readonly Texture texture;
        private int frameIndex;

        public TextureFrameSource(Texture texture)
        {
            this.texture = texture;
        }

        public bool IsReady => texture != null;
        public Vector2Int SourceSize => texture != null ? new Vector2Int(texture.width, texture.height) : Vector2Int.zero;
        public VisionFrameSourceType SourceType => VisionFrameSourceType.Texture;

        public void Initialize()
        {
        }

        public bool TryGetFrame(out VisionFrame frame)
        {
            frame = CreateFrame(texture, ++frameIndex, SourceType);
            return frame.IsValid;
        }

        public void Dispose()
        {
        }

        internal static VisionFrame CreateFrame(Texture texture, int frameIndex, VisionFrameSourceType sourceType)
        {
            if (texture == null)
                return default;

            return new VisionFrame(
                texture,
                frameIndex,
                Time.realtimeSinceStartupAsDouble,
                new Vector2Int(texture.width, texture.height),
                sourceType);
        }
    }

    public sealed class RenderTextureFrameSource : IVisionFrameSource
    {
        private readonly RenderTexture renderTexture;
        private int frameIndex;

        public RenderTextureFrameSource(RenderTexture renderTexture)
        {
            this.renderTexture = renderTexture;
        }

        public bool IsReady => renderTexture != null && renderTexture.IsCreated();
        public Vector2Int SourceSize => renderTexture != null ? new Vector2Int(renderTexture.width, renderTexture.height) : Vector2Int.zero;
        public VisionFrameSourceType SourceType => VisionFrameSourceType.RenderTexture;

        public void Initialize()
        {
            if (renderTexture != null && !renderTexture.IsCreated())
                renderTexture.Create();
        }

        public bool TryGetFrame(out VisionFrame frame)
        {
            frame = TextureFrameSource.CreateFrame(renderTexture, ++frameIndex, SourceType);
            return frame.IsValid;
        }

        public void Dispose()
        {
        }
    }

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
                UnityEngine.Object.Destroy(targetTexture);
            }
        }
    }

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
            UnityEngine.Object.Destroy(texture);
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

    public sealed class VideoFrameSource : IVisionFrameSource
    {
        private readonly VideoPlayer videoPlayer;
        private readonly bool autoPlay;
        private int frameIndex;

        public VideoFrameSource(VideoPlayer videoPlayer, bool autoPlay = true)
        {
            this.videoPlayer = videoPlayer ?? throw new ArgumentNullException(nameof(videoPlayer));
            this.autoPlay = autoPlay;
        }

        public bool IsReady => videoPlayer != null && videoPlayer.texture != null;
        public Vector2Int SourceSize => IsReady ? new Vector2Int(videoPlayer.texture.width, videoPlayer.texture.height) : Vector2Int.zero;
        public VisionFrameSourceType SourceType => VisionFrameSourceType.Video;

        public void Initialize()
        {
            if (videoPlayer.isPrepared)
            {
                if (autoPlay && !videoPlayer.isPlaying)
                    videoPlayer.Play();
                return;
            }

            videoPlayer.prepareCompleted += HandlePrepared;
            videoPlayer.Prepare();
        }

        public bool TryGetFrame(out VisionFrame frame)
        {
            frame = TextureFrameSource.CreateFrame(videoPlayer.texture, ++frameIndex, SourceType);
            return frame.IsValid;
        }

        public void Dispose()
        {
            if (videoPlayer == null)
                return;

            videoPlayer.prepareCompleted -= HandlePrepared;
            if (videoPlayer.isPlaying)
                videoPlayer.Stop();
        }

        private void HandlePrepared(VideoPlayer source)
        {
            if (autoPlay && source != null && !source.isPlaying)
                source.Play();
        }
    }
}
