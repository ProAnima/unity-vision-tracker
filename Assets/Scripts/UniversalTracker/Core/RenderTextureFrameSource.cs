using UnityEngine;

namespace UniversalTracker.Core
{
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
}
