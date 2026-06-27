using UnityEngine;
using System.Collections.Generic;
using UniversalTracker.Core;

namespace UniversalTracker.Models
{
    /// <summary>
    /// Модель детекции YOLO11 (объекты, люди)
    /// </summary>
    public class YOLO11DetectionModel : BaseInferenceModel
    {
        #region Public Properties
        
        public override ModelType ModelType => ModelType.Detection;
        
        #endregion
        
        #region Public Methods
        
        public override InferenceResult RunInference(Texture inputTexture)
        {
            if (!IsInitialized)
            {
                Debug.LogError("❌ [YOLO11] Модель не инициализирована!");
                return CreateFailedResult();
            }
            
            if (inputTexture == null)
            {
                Debug.LogError("❌ [YOLO11] Входная текстура null!");
                return CreateFailedResult();
            }
            
            try
            {
                var startTime = Time.realtimeSinceStartup;
                
                var input = PrepareInput(inputTexture);
                if (input == null)
                {
                    Debug.LogError("❌ [YOLO11] PrepareInput вернул null!");
                    return CreateFailedResult();
                }
                
                var outputData = ExecuteInference();
                if (outputData == null || outputData.Length == 0)
                {
                    Debug.LogError("❌ [YOLO11] ExecuteInference вернул null или пустой массив!");
                    return CreateFailedResult();
                }
                
                var boxes = ParseYOLOOutput(outputData);
                
                if (boxes != null && boxes.Length > 0)
                {
                    boxes = nmsProcessor.ApplyNMS(boxes, config.nmsThreshold);
                    boxes = postProcessor.FilterByConfidence(boxes, config.confidenceThreshold);
                    
                    // НЕ трансформируем координаты - оставляем normalized (0-1) для UI!
                    // Visualizer сам сделает flip Y при отрисовке
                }
                
                var inferenceTime = (Time.realtimeSinceStartup - startTime) * 1000f;
                
                return new InferenceResult
                {
                    success = true,
                    modelType = ModelType.Detection,
                    inferenceTime = inferenceTime,
                    detectionCount = boxes != null ? boxes.Length : 0,
                    boundingBoxes = boxes ?? new BBoxData[0],
                    maxConfidence = boxes != null && boxes.Length > 0 ? boxes[0].confidence : 0f
                };
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ [YOLO11] КРИТИЧЕСКАЯ ОШИБКА RunInference: {e.Message}\n{e.StackTrace}");
                return CreateFailedResult();
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private BBoxData[] ParseYOLOOutput(float[] data)
        {
            var candidates = new List<BBoxData>();
            
            // YOLO output: [1, 84, 8400] = [batch, (4 bbox + 80 classes), anchors]
            int channels = 84;
            int anchors = data.Length / channels;
            
            for (int i = 0; i < anchors; i++)
            {
                float maxScore = 0f;
                int bestClass = -1;
                
                for (int c = 4; c < channels; c++)
                {
                    float score = data[c * anchors + i];
                    if (score > maxScore)
                    {
                        maxScore = score;
                        bestClass = c - 4;
                    }
                }
                
                if (maxScore > 0.15f)
                {
                    float cx = data[0 * anchors + i] / config.inputSize;
                    float cy = data[1 * anchors + i] / config.inputSize;
                    float w = data[2 * anchors + i] / config.inputSize;
                    float h = data[3 * anchors + i] / config.inputSize;
                    
                    float x = cx - w / 2f;
                    float y = cy - h / 2f;
                    
                    candidates.Add(new BBoxData
                    {
                        classId = bestClass,
                        className = GetClassName(bestClass),
                        confidence = maxScore,
                        rect = new Rect(x, y, w, h)
                    });
                }
            }
            
            return candidates.ToArray();
        }
        
        private string GetClassName(int classId)
        {
            // COCO классы (упрощенная версия)
            if (classId == 0) return "person";
            return $"class_{classId}";
        }
        
        private InferenceResult CreateFailedResult() =>
            new InferenceResult { success = false, modelType = ModelType.Detection };
        
        #endregion
    }
}
