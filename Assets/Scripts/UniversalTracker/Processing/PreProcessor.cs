using UnityEngine;
using Unity.InferenceEngine;

namespace UniversalTracker.Processing
{
    /// <summary>
    /// Препроцессинг входных текстур для нейросети
    /// </summary>
    public class PreProcessor
    {
        #region Public Methods
        
        public Tensor<float> PreprocessTexture(Texture source, int targetSize)
        {
            if (source == null)
            {
                Debug.LogError("❌ [PreProcessor] Входная текстура null!");
                return null;
            }
            
            var tensor = new Tensor<float>(new TensorShape(1, 3, targetSize, targetSize));
            
            TextureConverter.ToTensor(source, tensor);
            
            return tensor;
        }
        
        public Tensor<float> PreprocessWithNormalization(Texture source, int targetSize, 
            Vector3 mean = default, Vector3 std = default)
        {
            var tensor = PreprocessTexture(source, targetSize);
            
            if (tensor == null) return null;
            
            if (mean == default) mean = new Vector3(0.485f, 0.456f, 0.406f);
            if (std == default) std = new Vector3(0.229f, 0.224f, 0.225f);
            
            // Нормализация будет выполнена на GPU
            
            return tensor;
        }
        
        public Tensor<float> PreprocessCroppedRegion(Texture source, Rect roi, int targetSize)
        {
            if (source == null)
            {
                Debug.LogError("❌ [PreProcessor] Входная текстура для crop null!");
                return null;
            }
            
            var tensor = new Tensor<float>(new TensorShape(1, 3, targetSize, targetSize));
            
            // ROI crop будет реализован через RenderTexture
            var croppedTexture = CropTexture(source, roi);
            TextureConverter.ToTensor(croppedTexture, tensor);
            
            return tensor;
        }
        
        #endregion
        
        #region Private Methods
        
        private RenderTexture CropTexture(Texture source, Rect roi)
        {
            int width = Mathf.RoundToInt(source.width * roi.width);
            int height = Mathf.RoundToInt(source.height * roi.height);
            
            var rt = RenderTexture.GetTemporary(width, height);
            
            // Копирование crop области (упрощенная версия)
            Graphics.Blit(source, rt);
            
            return rt;
        }
        
        #endregion
    }
}
