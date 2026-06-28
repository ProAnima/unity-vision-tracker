using System;
using UnityEngine;
using UnityEngine.Video;

namespace UniversalTracker.Core
{
    internal readonly struct VisionFrameSourceRequest
    {
        public readonly InputProviderType inputType;
        public readonly MonoBehaviour customInputProvider;
        public readonly Texture sourceTexture;
        public readonly RenderTexture sourceRenderTexture;
        public readonly Camera sourceCamera;
        public readonly RenderTexture cameraTargetTexture;
        public readonly VideoPlayer sourceVideoPlayer;
        public readonly string webCamDeviceName;
        public readonly int webCamRequestedWidth;
        public readonly int webCamRequestedHeight;
        public readonly int webCamRequestedFps;
        public readonly Func<VideoPlayer> componentVideoPlayerProvider;
        public readonly Action<RenderTexture> cameraTargetTextureCreated;

        public VisionFrameSourceRequest(
            InputProviderType inputType,
            MonoBehaviour customInputProvider,
            Texture sourceTexture,
            RenderTexture sourceRenderTexture,
            Camera sourceCamera,
            RenderTexture cameraTargetTexture,
            VideoPlayer sourceVideoPlayer,
            string webCamDeviceName,
            int webCamRequestedWidth,
            int webCamRequestedHeight,
            int webCamRequestedFps,
            Func<VideoPlayer> componentVideoPlayerProvider,
            Action<RenderTexture> cameraTargetTextureCreated)
        {
            this.inputType = inputType;
            this.customInputProvider = customInputProvider;
            this.sourceTexture = sourceTexture;
            this.sourceRenderTexture = sourceRenderTexture;
            this.sourceCamera = sourceCamera;
            this.cameraTargetTexture = cameraTargetTexture;
            this.sourceVideoPlayer = sourceVideoPlayer;
            this.webCamDeviceName = webCamDeviceName;
            this.webCamRequestedWidth = webCamRequestedWidth;
            this.webCamRequestedHeight = webCamRequestedHeight;
            this.webCamRequestedFps = webCamRequestedFps;
            this.componentVideoPlayerProvider = componentVideoPlayerProvider;
            this.cameraTargetTextureCreated = cameraTargetTextureCreated;
        }
    }

    internal static class VisionFrameSourceFactory
    {
        public static IVisionFrameSource Create(VisionFrameSourceRequest request)
        {
            if (request.customInputProvider is IVisionFrameSource customSource)
                return customSource;

            return request.inputType switch
            {
                InputProviderType.Texture => CreateTextureSource(request),
                InputProviderType.RenderTexture => new RenderTextureFrameSource(request.sourceRenderTexture),
                InputProviderType.Camera => CreateCameraSource(request),
                InputProviderType.Video => CreateVideoSource(request),
                InputProviderType.WebCam => CreateWebCamSource(request),
                _ => null
            };
        }

        private static IVisionFrameSource CreateWebCamSource(VisionFrameSourceRequest request)
        {
            return new WebCamFrameSource(
                request.webCamDeviceName,
                Mathf.Max(16, request.webCamRequestedWidth),
                Mathf.Max(16, request.webCamRequestedHeight),
                Mathf.Max(1, request.webCamRequestedFps));
        }

        private static IVisionFrameSource CreateTextureSource(VisionFrameSourceRequest request)
        {
            return request.sourceRenderTexture != null
                ? new RenderTextureFrameSource(request.sourceRenderTexture)
                : new TextureFrameSource(request.sourceTexture);
        }

        private static IVisionFrameSource CreateCameraSource(VisionFrameSourceRequest request)
        {
            bool ownsTargetTexture = request.cameraTargetTexture == null;
            RenderTexture targetTexture = request.cameraTargetTexture ?? CreateDefaultCameraTargetTexture();
            if (ownsTargetTexture)
                request.cameraTargetTextureCreated?.Invoke(targetTexture);

            return new UnityCameraFrameSource(
                request.sourceCamera != null ? request.sourceCamera : Camera.main,
                targetTexture,
                ownsTargetTexture);
        }

        private static RenderTexture CreateDefaultCameraTargetTexture()
        {
            return new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
        }

        private static IVisionFrameSource CreateVideoSource(VisionFrameSourceRequest request)
        {
            VideoPlayer player = request.sourceVideoPlayer != null
                ? request.sourceVideoPlayer
                : request.componentVideoPlayerProvider?.Invoke();

            return player != null ? new VideoFrameSource(player) : null;
        }
    }
}
