using UnityEngine;
using UniversalTracker.Core;

namespace UniversalTracker.Models
{
    /// <summary>
    /// Модель сегментации (YOLO11-seg, SAM, FastSAM)
    /// </summary>
    public class SegmentationModel : BaseInferenceModel
    {
        #region Public Properties
        
        public override ModelType ModelType => ModelType.Segmentation;
        
        #endregion
        
        #region Public Methods
        
        public override InferenceResult RunInference(Texture inputTexture)
        {
            if (!IsInitialized)
            {
                Debug.LogError("❌ [Segmentation] Модель не инициализирована!");
                return CreateFailedResult();
            }
            
            var startTime = Time.realtimeSinceStartup;
            
            var input = PrepareInput(inputTexture);
            if (input == null)
                return CreateFailedResult();
            
            var outputData = ExecuteInference();
            if (outputData == null)
                return CreateFailedResult();
            
            // Парсинг масок (упрощенная версия, зависит от модели)
            var masks = ParseSegmentationOutput(outputData);
            
            var inferenceTime = (Time.realtimeSinceStartup - startTime) * 1000f;
            
            return new InferenceResult
            {
                success = true,
                modelType = ModelType.Segmentation,
                inferenceTime = inferenceTime,
                detectionCount = masks.Length,
                masks = masks,
                maxConfidence = masks.Length > 0 ? masks[0].confidence : 0f
            };
        }
        
        #endregion
        
        #region Private Methods
        
        private MaskData[] ParseSegmentationOutput(float[] data)
        {
            // TODO: Реализовать парсинг масок в зависимости от конкретной модели
            // YOLO-seg выдает маски размером [N, H, W]
            // SAM выдает [1, H, W] маску
            
            Debug.LogWarning("⚠️ [Segmentation] Парсинг масок еще не реализован полностью");
            
            return new MaskData[0];
        }
        
        private InferenceResult CreateFailedResult() =>
            new InferenceResult { success = false, modelType = ModelType.Segmentation };
        
        #endregion
    }
}
