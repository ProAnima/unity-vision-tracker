using UnityEngine;
using System.Collections.Generic;
using UniversalTracker.Core;

namespace UniversalTracker.Models
{
    /// <summary>
    /// Модель pose estimation YOLO11 (скелеты людей)
    /// </summary>
    public class YOLO11PoseModel : BaseInferenceModel
    {
        #region Public Properties
        
        public override ModelType ModelType => ModelType.Pose;
        
        #endregion
        
        #region Public Methods
        
        public override InferenceResult RunInference(Texture inputTexture)
        {
            if (!IsInitialized)
            {
                Debug.LogError("❌ [YOLO11Pose] Модель не инициализирована!");
                return CreateFailedResult();
            }
            
            var startTime = Time.realtimeSinceStartup;
            
            var input = PrepareInput(inputTexture);
            if (input == null)
                return CreateFailedResult();
            
            var outputData = ExecuteInference();
            if (outputData == null)
                return CreateFailedResult();
            
            var (boxes, keypoints) = ParseYOLOPoseOutput(outputData);
            
            boxes = nmsProcessor.ApplyNMS(boxes, config.nmsThreshold);
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
        
        private (BBoxData[], KeypointData[]) ParseYOLOPoseOutput(float[] data)
        {
            var boxes = new List<BBoxData>();
            var keypoints = new List<KeypointData>();
            
            // YOLO-Pose output: [1, 56, 8400] = [batch, (4 bbox + 1 conf + 17*3 keypoints), anchors]
            // 4 (bbox) + 1 (person conf) + 51 (17 keypoints * 3: x,y,visibility)
            int channels = 56;
            int anchors = data.Length / channels;
            int numKeypoints = 17;
            
            for (int i = 0; i < anchors; i++)
            {
                float personConf = data[4 * anchors + i];
                
                if (personConf > 0.15f)
                {
                    float cx = data[0 * anchors + i] / config.inputSize;
                    float cy = data[1 * anchors + i] / config.inputSize;
                    float w = data[2 * anchors + i] / config.inputSize;
                    float h = data[3 * anchors + i] / config.inputSize;
                    
                    float x = cx - w / 2f;
                    float y = cy - h / 2f;
                    
                    var box = new BBoxData
                    {
                        id = boxes.Count,
                        classId = 0,
                        className = "person",
                        confidence = personConf,
                        rect = new Rect(x, y, w, h)
                    };
                    
                    var kp = new KeypointData
                    {
                        personId = boxes.Count,
                        points = new Vector2[numKeypoints],
                        confidences = new float[numKeypoints],
                        connections = GetCOCOConnections()
                    };
                    
                    for (int k = 0; k < numKeypoints; k++)
                    {
                        int baseIdx = 5 + k * 3;
                        float kpX = data[baseIdx * anchors + i] / config.inputSize;
                        float kpY = data[(baseIdx + 1) * anchors + i] / config.inputSize;
                        float kpConf = data[(baseIdx + 2) * anchors + i];
                        
                        kp.points[k] = new Vector2(kpX, kpY);
                        kp.confidences[k] = kpConf;
                    }
                    
                    boxes.Add(box);
                    keypoints.Add(kp);
                }
            }
            
            return (boxes.ToArray(), keypoints.ToArray());
        }
        
        private int[] GetCOCOConnections()
        {
            // COCO 17 keypoints connections (пары индексов)
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
