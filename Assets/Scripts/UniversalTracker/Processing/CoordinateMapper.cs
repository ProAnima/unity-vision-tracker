using UnityEngine;
using UniversalTracker.Core;

namespace UniversalTracker.Processing
{
    /// <summary>
    /// Маппинг координат между различными пространствами (текстура, экран, мир)
    /// </summary>
    public class CoordinateMapper
    {
        #region Public Methods
        
        public Vector2 NormalizedToPixel(Vector2 normalized, Vector2Int resolution) =>
            new Vector2(normalized.x * resolution.x, normalized.y * resolution.y);
        
        public Vector2 PixelToNormalized(Vector2 pixel, Vector2Int resolution) =>
            new Vector2(pixel.x / resolution.x, pixel.y / resolution.y);
        
        public Rect NormalizedToPixelRect(Rect normalized, Vector2Int resolution)
        {
            return new Rect(
                normalized.x * resolution.x,
                normalized.y * resolution.y,
                normalized.width * resolution.x,
                normalized.height * resolution.y
            );
        }
        
        public Rect PixelToNormalizedRect(Rect pixel, Vector2Int resolution)
        {
            return new Rect(
                pixel.x / resolution.x,
                pixel.y / resolution.y,
                pixel.width / resolution.x,
                pixel.height / resolution.y
            );
        }
        
        public Vector3 TextureToWorldSpace(Vector2 textureCoord, Camera camera, float depth)
        {
            if (camera == null)
            {
                Debug.LogWarning("⚠️ [CoordinateMapper] Камера null, не могу конвертировать в world space!");
                return Vector3.zero;
            }
            
            var screenPoint = new Vector3(textureCoord.x * Screen.width, textureCoord.y * Screen.height, depth);
            return camera.ScreenToWorldPoint(screenPoint);
        }
        
        public Vector2 WorldToTextureSpace(Vector3 worldPos, Camera camera, Vector2Int resolution)
        {
            if (camera == null)
            {
                Debug.LogWarning("⚠️ [CoordinateMapper] Камера null, не могу конвертировать в texture space!");
                return Vector2.zero;
            }
            
            var screenPoint = camera.WorldToScreenPoint(worldPos);
            return new Vector2(screenPoint.x / resolution.x, screenPoint.y / resolution.y);
        }
        
        public Rect ExpandRect(Rect rect, float margin)
        {
            return new Rect(
                rect.x - margin,
                rect.y - margin,
                rect.width + margin * 2f,
                rect.height + margin * 2f
            );
        }
        
        public Rect ClampRect(Rect rect, Rect bounds)
        {
            var clamped = rect;
            
            clamped.xMin = Mathf.Max(rect.xMin, bounds.xMin);
            clamped.yMin = Mathf.Max(rect.yMin, bounds.yMin);
            clamped.xMax = Mathf.Min(rect.xMax, bounds.xMax);
            clamped.yMax = Mathf.Min(rect.yMax, bounds.yMax);
            
            return clamped;
        }
        
        #endregion
    }
}
