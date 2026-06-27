using UnityEngine;
using UniversalTracker.Core;

namespace UniversalTracker.Processing
{
    /// <summary>
    /// Постобработка результатов нейросети
    /// </summary>
    public class PostProcessor
    {
        #region Public Methods
        
        public void ApplyCoordinateTransform(BBoxData[] boxes, ModelConfig config, Vector2Int resolution)
        {
            if (boxes == null) return;
            
            foreach (var box in boxes)
                ApplyTransformToBox(box, config, resolution);
        }
        
        public void ApplyCoordinateTransform(KeypointData[] keypoints, ModelConfig config, Vector2Int resolution)
        {
            if (keypoints == null) return;
            
            foreach (var kp in keypoints)
            {
                if (kp.points == null) continue;
                
                for (int i = 0; i < kp.points.Length; i++)
                    kp.points[i] = TransformPoint(kp.points[i], config, resolution);
            }
        }
        
        public BBoxData[] FilterByConfidence(BBoxData[] boxes, float threshold)
        {
            if (boxes == null) return null;
            
            var filtered = new System.Collections.Generic.List<BBoxData>();
            
            foreach (var box in boxes)
            {
                if (box.confidence >= threshold)
                    filtered.Add(box);
            }
            
            return filtered.ToArray();
        }
        
        public void NormalizeCoordinates(BBoxData[] boxes, int inputSize)
        {
            if (boxes == null) return;
            
            float invSize = 1f / inputSize;
            
            foreach (var box in boxes)
            {
                box.rect.x *= invSize;
                box.rect.y *= invSize;
                box.rect.width *= invSize;
                box.rect.height *= invSize;
                box.center = box.rect.center;
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void ApplyTransformToBox(BBoxData box, ModelConfig config, Vector2Int resolution)
        {
            var rect = box.rect;
            
            // Flip
            if (config.flipX)
                rect.x = 1f - (rect.x + rect.width);
            
            if (config.flipY)
                rect.y = 1f - (rect.y + rect.height);
            
            // Scale
            var center = rect.center;
            center = new Vector2(
                0.5f + (center.x - 0.5f) * config.scale.x,
                0.5f + (center.y - 0.5f) * config.scale.y
            );
            
            rect.center = center;
            
            // Offset
            rect.x += config.offset.x;
            rect.y += config.offset.y;
            
            box.rect = rect;
            box.center = rect.center;
        }
        
        private Vector2 TransformPoint(Vector2 point, ModelConfig config, Vector2Int resolution)
        {
            // Flip
            if (config.flipX)
                point.x = 1f - point.x;
            
            if (config.flipY)
                point.y = 1f - point.y;
            
            // Scale
            point = new Vector2(
                0.5f + (point.x - 0.5f) * config.scale.x,
                0.5f + (point.y - 0.5f) * config.scale.y
            );
            
            // Offset
            point += config.offset;
            
            return point;
        }
        
        #endregion
    }
}
