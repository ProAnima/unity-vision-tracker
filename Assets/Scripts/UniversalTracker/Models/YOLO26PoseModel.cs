using UnityEngine;
using System.Collections.Generic;
using UniversalTracker.Core;
using Unity.InferenceEngine;

namespace UniversalTracker.Models
{
    /// <summary>
    /// Модель pose estimation YOLO26 (NMS-free, оптимизирована для edge)
    /// </summary>
    public class YOLO26PoseModel : BaseInferenceModel
    {
        #region Public Properties
        
        public override ModelType ModelType => ModelType.Pose;
        
        #endregion
        
        #region Public Methods
        
        public override InferenceResult RunInference(Texture inputTexture)
        {
            if (!IsInitialized)
            {
                Debug.LogError("❌ [YOLO26Pose] Модель не инициализирована!");
                return CreateFailedResult();
            }
            
            var startTime = Time.realtimeSinceStartup;
            
            var input = PrepareInput(inputTexture);
            if (input == null)
                return CreateFailedResult();
            
            var (outputData, shape) = ExecuteInferenceWithShape();
            if (outputData == null || outputData.Length == 0)
                return CreateFailedResult();
            
            var (boxes, keypoints) = ParseYOLO26PoseOutput(outputData, shape);
            
            boxes = postProcessor.FilterByConfidence(boxes, config.confidenceThreshold);
            
            // НЕ трансформируем координаты - оставляем normalized (0-1) для UI!
            // Visualizer сам сделает flip Y при отрисовке
            
            var inferenceTime = (Time.realtimeSinceStartup - startTime) * 1000f;
            
            return new InferenceResult
            {
                success = true,
                modelType = ModelType.Pose,
                inferenceTime = inferenceTime,
                detectionCount = keypoints.Length,
                boundingBoxes = boxes,
                keypoints = keypoints,
                maxConfidence = boxes.Length > 0 ? boxes[0].confidence : 0f
            };
        }
        
        #endregion
        
        #region Private Methods
        
        private (BBoxData[], KeypointData[]) ParseYOLO26PoseOutput(float[] data, TensorShape shape)
        {
            var boxes = new List<BBoxData>();
            var keypoints = new List<KeypointData>();
            
            Debug.Log($"📊 [YOLO26Pose] ParseOutput: shape = {shape}, data.Length = {data.Length}");
            
            // YOLO26-Pose output: [batch, num_detections, N]
            // N = 4 (bbox) + 1 (conf) + 17*3 (keypoints) = 56
            // Layout в flat array: [detection][feature] (детекции идут последовательно!)
            
            int numKeypoints = 17;
            int featureSize = 4 + 1 + numKeypoints * 3; // 56
            
            int batch = shape[0];
            int numDetections = shape[1];
            int channels = shape[2];
            
            Debug.Log($"   batch={batch}, numDetections={numDetections}, channels={channels}");
            
            // Debug первых значений
            if (data.Length >= 10)
            {
                Debug.Log($"   RAW data[0..9]: {data[0]:F1}, {data[1]:F1}, {data[2]:F1}, {data[3]:F1}, {data[4]:F2}, {data[5]:F1}, {data[6]:F1}, {data[7]:F1}, {data[8]:F2}, {data[9]:F1}");
            }
            
            int validDetections = 0;
            float invSize = 1f / config.inputSize;
            
            // Layout: [detection][feature]
            for (int i = 0; i < numDetections; i++)
            {
                int offset = i * channels; // Каждая детекция занимает channels (56) элементов подряд
                
                if (offset + channels > data.Length)
                    break;
                
                float x1_px = data[offset + 0];
                float y1_px = data[offset + 1];
                float x2_px = data[offset + 2];
                float y2_px = data[offset + 3];
                float confidence = data[offset + 4];
                
                // Фильтруем пустые слоты
                if (confidence < 0.15f || confidence > 1.0f)
                    continue;
                
                // Проверка валидности bbox
                if (x1_px < 0 || y1_px < 0 || x2_px < 0 || y2_px < 0)
                    continue;
                if (x1_px > config.inputSize || x2_px > config.inputSize)
                    continue;
                if (x2_px <= x1_px || y2_px <= y1_px) // Bbox должен иметь положительные размеры
                    continue;
                
                validDetections++;
                
                // Нормализуем
                float x1 = x1_px * invSize;
                float y1 = y1_px * invSize;
                float x2 = x2_px * invSize;
                float y2 = y2_px * invSize;
                
                float x = x1;
                float y = y1;
                float w = x2 - x1;
                float h = y2 - y1;
                
                if (validDetections <= 3)
                    Debug.Log($"   Person #{validDetections}: conf={confidence:F2}, bbox_px=({x1_px:F0}, {y1_px:F0}, {x2_px:F0}, {y2_px:F0}) → norm=({x:F3}, {y:F3}, {w:F3}, {h:F3})");
                
                var box = new BBoxData
                {
                    id = boxes.Count,
                    classId = 0,
                    className = "person",
                    confidence = confidence,
                    rect = new Rect(x, y, w, h)
                };
                
                var kp = new KeypointData
                {
                    personId = boxes.Count,
                    points = new Vector2[numKeypoints],
                    confidences = new float[numKeypoints],
                    connections = GetCOCOConnections()
                };
                
                // Парсим keypoints (layout: последовательно после bbox+conf)
                // ВАЖНО: формат [confidence, x, y] для каждой точки, а не [x, y, confidence]!
                int validKP = 0;
                for (int k = 0; k < numKeypoints; k++)
                {
                    int kpOffset = offset + 5 + k * 3; // 5 = bbox(4) + conf(1)
                    
                    // Правильный порядок: [conf, x, y]
                    float kpConf = data[kpOffset + 0];
                    float kpX_px = data[kpOffset + 1];
                    float kpY_px = data[kpOffset + 2];
                    
                    // Debug RAW для первых 3 keypoints
                    if (validDetections == 1 && k < 3)
                        Debug.Log($"      KP[{k}] RAW: conf={data[kpOffset]:F2}, x={data[kpOffset+1]:F1}, y={data[kpOffset+2]:F1}");
                    
                    // Нормализуем
                    float kpX = kpX_px * invSize;
                    float kpY = kpY_px * invSize;
                    
                    // Clamp для безопасности
                    kpX = Mathf.Clamp01(kpX);
                    kpY = Mathf.Clamp01(kpY);
                    kpConf = Mathf.Clamp01(kpConf);
                    
                    kp.points[k] = new Vector2(kpX, kpY);
                    kp.confidences[k] = kpConf;
                    
                    if (kpConf > 0.5f) validKP++;
                    
                    // Debug первых 3 keypoints первого человека
                    if (validDetections == 1 && k < 3)
                        Debug.Log($"      KP[{k}]: px=({kpX_px:F1}, {kpY_px:F1}) conf={kpConf:F2} → norm=({kpX:F3}, {kpY:F3})");
                }
                
                if (validDetections <= 3)
                    Debug.Log($"      → Валидных keypoints: {validKP}/{numKeypoints}");
                
                boxes.Add(box);
                keypoints.Add(kp);
            }
            
            Debug.Log($"✅ [YOLO26Pose] Найдено детекций: {boxes.Count}");
            
            return (boxes.ToArray(), keypoints.ToArray());
        }
        
        private int[] GetCOCOConnections()
        {
            // COCO 17 keypoints connections
            return new int[]
            {
                0, 1,   0, 2,   1, 3,   2, 4,   // Голова
                5, 6,   5, 7,   7, 9,   6, 8,   8, 10, // Руки
                5, 11,  6, 12,  11, 12, // Торс
                11, 13, 13, 15, 12, 14, 14, 16  // Ноги
            };
        }
        
        private InferenceResult CreateFailedResult() =>
            new InferenceResult { success = false, modelType = ModelType.Pose };
        
        #endregion
    }
}
