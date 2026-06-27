using UnityEngine;
using System.Collections.Generic;
using UniversalTracker.Core;

namespace UniversalTracker.Models
{
    /// <summary>
    /// Модель детекции YOLO26 (NMS-free, оптимизирована для edge)
    /// </summary>
    public class YOLO26DetectionModel : BaseInferenceModel
    {
        #region Public Properties
        
        public override ModelType ModelType => ModelType.Detection;
        
        #endregion
        
        #region Public Methods
        
        public override InferenceResult RunInference(Texture inputTexture)
        {
            if (!IsInitialized)
            {
                Debug.LogError("❌ [YOLO26] Модель не инициализирована!");
                return CreateFailedResult();
            }
            
            var startTime = Time.realtimeSinceStartup;
            
            var input = PrepareInput(inputTexture);
            if (input == null)
                return CreateFailedResult();
            
            var outputData = ExecuteInference();
            if (outputData == null)
                return CreateFailedResult();
            
            // YOLO26 выдает уже отфильтрованные детекции (NMS-free)
            var boxes = ParseYOLO26Output(outputData);
            
            // Только фильтрация по confidence и координатная трансформация
            boxes = postProcessor.FilterByConfidence(boxes, config.confidenceThreshold);
            postProcessor.ApplyCoordinateTransform(boxes, config, new Vector2Int(config.inputSize, config.inputSize));
            
            var inferenceTime = (Time.realtimeSinceStartup - startTime) * 1000f;
            
            return new InferenceResult
            {
                success = true,
                modelType = ModelType.Detection,
                inferenceTime = inferenceTime,
                detectionCount = boxes.Length,
                boundingBoxes = boxes,
                maxConfidence = boxes.Length > 0 ? boxes[0].confidence : 0f
            };
        }
        
        #endregion
        
        #region Private Methods
        
        private BBoxData[] ParseYOLO26Output(float[] data)
        {
            var detections = new List<BBoxData>();
            
            // YOLO26 output (NMS-free): [batch, num_detections, 6]
            // где 6 = [x1, y1, x2, y2, confidence, class_id]
            // или в некоторых версиях: [batch, num_detections, 85] для COCO
            
            int numDetections = DetermineNumDetections(data.Length);
            int stride = DetermineStride(data.Length, numDetections);
            
            for (int i = 0; i < numDetections; i++)
            {
                int offset = i * stride;
                
                if (offset + 5 >= data.Length)
                    break;
                
                float confidence = data[offset + 4];
                
                // YOLO26 уже отфильтровал низкие confidence внутри модели
                if (confidence > 0.01f)
                {
                    float x1 = data[offset + 0] / config.inputSize;
                    float y1 = data[offset + 1] / config.inputSize;
                    float x2 = data[offset + 2] / config.inputSize;
                    float y2 = data[offset + 3] / config.inputSize;
                    
                    int classId = 0;
                    
                    // Если есть классы, находим лучший
                    if (stride > 6)
                    {
                        float maxClassScore = 0f;
                        for (int c = 5; c < stride; c++)
                        {
                            float classScore = data[offset + c];
                            if (classScore > maxClassScore)
                            {
                                maxClassScore = classScore;
                                classId = c - 5;
                            }
                        }
                    }
                    else if (stride >= 6)
                    {
                        classId = (int)data[offset + 5];
                    }
                    
                    float x = x1;
                    float y = y1;
                    float w = x2 - x1;
                    float h = y2 - y1;
                    
                    detections.Add(new BBoxData
                    {
                        classId = classId,
                        className = GetClassName(classId),
                        confidence = confidence,
                        rect = new Rect(x, y, w, h)
                    });
                }
            }
            
            return detections.ToArray();
        }
        
        private int DetermineNumDetections(int dataLength)
        {
            // Автоопределение количества детекций по размеру выхода
            // YOLO26 обычно возвращает фиксированное число (например, 300 или 100)
            
            if (dataLength >= 25500) return 300; // 300 * 85 (COCO)
            if (dataLength >= 8500) return 100;  // 100 * 85
            if (dataLength >= 1800) return 300;  // 300 * 6 (простой формат)
            if (dataLength >= 600) return 100;   // 100 * 6
            
            return dataLength / 6; // По умолчанию
        }
        
        private int DetermineStride(int dataLength, int numDetections)
        {
            if (numDetections == 0) return 6;
            return dataLength / numDetections;
        }
        
        private string GetClassName(int classId)
        {
            // COCO классы (упрощенная версия)
            if (classId == 0) return "person";
            if (classId == 1) return "bicycle";
            if (classId == 2) return "car";
            return $"class_{classId}";
        }
        
        private InferenceResult CreateFailedResult() =>
            new InferenceResult { success = false, modelType = ModelType.Detection };
        
        #endregion
    }
}
