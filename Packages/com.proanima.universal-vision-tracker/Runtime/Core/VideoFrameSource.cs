using System;
using UnityEngine;
using UnityEngine.Video;

namespace UniversalTracker.Core
{
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
