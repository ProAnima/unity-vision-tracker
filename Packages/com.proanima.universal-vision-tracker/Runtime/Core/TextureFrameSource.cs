using UnityEngine;

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
}
