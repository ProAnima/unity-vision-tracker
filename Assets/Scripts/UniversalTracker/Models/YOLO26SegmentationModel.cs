using UnityEngine;
using Unity.InferenceEngine;
using System.Collections.Generic;
using UniversalTracker.Core;

namespace UniversalTracker.Models
{
    /// <summary>
    /// Модель сегментации YOLO26 (instance segmentation)
    /// </summary>
    public class YOLO26SegmentationModel : BaseInferenceModel
    {
        #region Public Properties
        
        public override ModelType ModelType => ModelType.Segmentation;
        
        #endregion
        
        #region Private Fields
        
        private InferenceResult lastResult; // Кэш для очистки текстур
        
        #endregion
        
        #region Public Methods
        
        public override InferenceResult RunInference(Texture inputTexture)
        {
            if (!IsInitialized)
            {
                Debug.LogError("❌ [YOLO26Seg] Модель не инициализирована!");
                return CreateFailedResult();
            }
            
            var startTime = Time.realtimeSinceStartup;
            
            // Очистка старых текстур масок перед созданием новых
            CleanupOldMasks();
            
            var input = PrepareInput(inputTexture);
            if (input == null)
                return CreateFailedResult();
            
            // YOLO26-seg имеет 2 выхода: [detections] и [proto_masks]
            var outputs = ExecuteInferenceMultiOutput();
            
            if (outputs == null || outputs.Count < 2)
            {
                Debug.LogError($"❌ [YOLO26Seg] Ожидалось 2 выхода, получено {outputs?.Count ?? 0}");
                return CreateFailedResult();
            }
            
            var (detectionsData, detectionsShape) = outputs[0];
            var (protoData, protoShape) = outputs[1];
            
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"📊 [YOLO26Seg] Output 0 (detections): shape={detectionsShape}");
                Debug.Log($"📊 [YOLO26Seg] Output 1 (proto): shape={protoShape}");
            }
            
            var (boxes, masks) = ParseYOLO26SegOutput(detectionsData, detectionsShape, protoData, protoShape);
            
            boxes = postProcessor.FilterByConfidence(boxes, config.confidenceThreshold);
            
            var inferenceTime = (Time.realtimeSinceStartup - startTime) * 1000f;
            
            var result = new InferenceResult
            {
                success = true,
                modelType = ModelType.Segmentation,
                inferenceTime = inferenceTime,
                detectionCount = boxes.Length,
                boundingBoxes = boxes,
                masks = masks,
                maxConfidence = boxes.Length > 0 ? boxes[0].confidence : 0f
            };
            
            lastResult = result; // Сохраняем для очистки в следующем кадре
            return result;
        }
        
        #endregion
        
        #region Private Methods
        
        private (BBoxData[], MaskData[]) ParseYOLO26SegOutput(float[] detectionsData, TensorShape detectionsShape, 
                                                               float[] protoData, TensorShape protoShape)
        {
            var boxes = new List<BBoxData>();
            var masks = new List<MaskData>();
            
            Debug.Log($"📊 [YOLO26Seg] Detections shape = {detectionsShape}, Proto shape = {protoShape}");
            
            // Detections: [batch, num_detections, N] где N = 4 (bbox) + 1 (conf) + 1 (class) + 32 (mask coeffs)
            int batch = detectionsShape[0];
            int numDetections = detectionsShape[1];
            int channels = detectionsShape[2]; // Должно быть 38 = 4+1+1+32
            
            // Proto masks: [batch, 32, H, W] обычно [1, 32, 160, 160]
            int numProtos = protoShape[1]; // 32
            int protoH = protoShape[2];     // 160
            int protoW = protoShape[3];     // 160
            
            Debug.Log($"   Detections: batch={batch}, numDet={numDetections}, channels={channels}");
            Debug.Log($"   Proto: numProtos={numProtos}, size={protoH}x{protoW}");
            
            if (channels != 38)
            {
                Debug.LogWarning($"⚠️ [YOLO26Seg] Ожидалось 38 каналов (4+1+1+32), получено {channels}");
            }
            
            int validDetections = 0;
            float invSize = 1f / config.inputSize;
            
            for (int i = 0; i < numDetections; i++)
            {
                int offset = i * channels;
                
                if (offset + channels > detectionsData.Length)
                    break;
                
                float x1_px = detectionsData[offset + 0];
                float y1_px = detectionsData[offset + 1];
                float x2_px = detectionsData[offset + 2];
                float y2_px = detectionsData[offset + 3];
                float confidence = detectionsData[offset + 4];
                int classId = (int)detectionsData[offset + 5];
                
                if (confidence < 0.15f || confidence > 1.0f)
                    continue;
                
                if (x1_px < 0 || y1_px < 0 || x2_px < 0 || y2_px < 0)
                    continue;
                if (x2_px <= x1_px || y2_px <= y1_px)
                    continue;
                
                validDetections++;
                
                // Нормализуем bbox
                float x1 = x1_px * invSize;
                float y1 = y1_px * invSize;
                float x2 = x2_px * invSize;
                float y2 = y2_px * invSize;
                
                float x = x1;
                float y = y1;
                float w = x2 - x1;
                float h = y2 - y1;
                
                if (validDetections <= 3)
                    Debug.Log($"   Object #{validDetections}: class={classId} ({GetClassName(classId)}), conf={confidence:F2}, bbox=({x:F3}, {y:F3}, {w:F3}, {h:F3})");
                
                var box = new BBoxData
                {
                    id = boxes.Count,
                    classId = classId,
                    className = GetClassName(classId),
                    confidence = confidence,
                    rect = new Rect(x, y, w, h)
                };
                
                // Mask coefficients (32 значения начиная с индекса 6)
                var maskCoeffs = new float[32];
                for (int k = 0; k < 32 && offset + 6 + k < detectionsData.Length; k++)
                {
                    maskCoeffs[k] = detectionsData[offset + 6 + k];
                }
                
                // Генерируем уникальный цвет для объекта
                Color objectColor = GetObjectColor(boxes.Count);
                
                // Восстановление маски из coefficients + proto
                var maskTexture = GenerateMaskTexture(maskCoeffs, protoData, protoH, protoW, 
                                                       x1_px, y1_px, x2_px, y2_px, objectColor);
                
                var mask = new MaskData
                {
                    objectId = boxes.Count,
                    confidence = confidence,
                    classId = classId,
                    className = GetClassName(classId),
                    bbox = new Rect(x, y, w, h),
                    maskCoefficients = maskCoeffs,
                    mask = maskTexture // Восстановленная маска
                };
                
                boxes.Add(box);
                masks.Add(mask);
            }
            
            Debug.Log($"✅ [YOLO26Seg] Найдено детекций: {boxes.Count}, масок: {masks.Count}");
            
            return (boxes.ToArray(), masks.ToArray());
        }
        
        private string GetClassName(int classId)
        {
            // COCO классы (упрощенная версия)
            string[] cocoClasses = new string[] {
                "person", "bicycle", "car", "motorcycle", "airplane", "bus", "train", "truck", "boat",
                "traffic light", "fire hydrant", "stop sign", "parking meter", "bench", "bird", "cat",
                "dog", "horse", "sheep", "cow", "elephant", "bear", "zebra", "giraffe", "backpack",
                "umbrella", "handbag", "tie", "suitcase", "frisbee", "skis", "snowboard", "sports ball",
                "kite", "baseball bat", "baseball glove", "skateboard", "surfboard", "tennis racket",
                "bottle", "wine glass", "cup", "fork", "knife", "spoon", "bowl", "banana", "apple",
                "sandwich", "orange", "broccoli", "carrot", "hot dog", "pizza", "donut", "cake",
                "chair", "couch", "potted plant", "bed", "dining table", "toilet", "tv", "laptop",
                "mouse", "remote", "keyboard", "cell phone", "microwave", "oven", "toaster", "sink",
                "refrigerator", "book", "clock", "vase", "scissors", "teddy bear", "hair drier", "toothbrush"
            };
            
            if (classId >= 0 && classId < cocoClasses.Length)
                return cocoClasses[classId];
            
            return $"class_{classId}";
        }
        
        /// <summary>
        /// Восстановление маски из coefficients + proto_masks
        /// mask = sigmoid(maskCoeffs @ proto_masks) > threshold
        /// </summary>
        private Texture2D GenerateMaskTexture(float[] maskCoeffs, float[] protoData, 
                                               int protoH, int protoW,
                                               float x1_px, float y1_px, float x2_px, float y2_px,
                                               Color maskColor)
        {
            try
            {
                // 1. Матричное умножение: maskCoeffs [32] @ proto [32, H, W] -> mask [H, W]
                float[] rawMask = new float[protoH * protoW];
                
                for (int y = 0; y < protoH; y++)
                {
                    for (int x = 0; x < protoW; x++)
                    {
                        float sum = 0f;
                        for (int c = 0; c < 32; c++)
                        {
                            int protoIdx = c * (protoH * protoW) + y * protoW + x;
                            if (protoIdx < protoData.Length)
                                sum += maskCoeffs[c] * protoData[protoIdx];
                        }
                        rawMask[y * protoW + x] = sum;
                    }
                }
                
                // 2. Применить sigmoid и threshold
                float threshold = 0.5f;
                for (int i = 0; i < rawMask.Length; i++)
                {
                    rawMask[i] = Sigmoid(rawMask[i]) > threshold ? 1f : 0f;
                }
                
                // 3. Обрезать по bbox и создать текстуру
                // Масштабируем bbox из inputSize координат в proto координаты (160x160)
                float scaleX = protoW / (float)config.inputSize;
                float scaleY = protoH / (float)config.inputSize;
                
                int bx1 = Mathf.Clamp((int)(x1_px * scaleX), 0, protoW - 1);
                int by1 = Mathf.Clamp((int)(y1_px * scaleY), 0, protoH - 1);
                int bx2 = Mathf.Clamp((int)(x2_px * scaleX), 0, protoW);
                int by2 = Mathf.Clamp((int)(y2_px * scaleY), 0, protoH);
                
                int maskW = Mathf.Max(1, bx2 - bx1);
                int maskH = Mathf.Max(1, by2 - by1);
                
                // Используем RGBA32 для цветной маски
                Texture2D texture = new Texture2D(maskW, maskH, TextureFormat.RGBA32, false);
                Color32[] pixels = new Color32[maskW * maskH];
                
                // Преобразуем цвет в Color32
                byte r = (byte)(maskColor.r * 255);
                byte g = (byte)(maskColor.g * 255);
                byte b = (byte)(maskColor.b * 255);
                
                for (int y = 0; y < maskH; y++)
                {
                    for (int x = 0; x < maskW; x++)
                    {
                        int px = bx1 + x;
                        int py = by1 + y;
                        
                        if (px >= 0 && px < protoW && py >= 0 && py < protoH)
                        {
                            byte alpha = rawMask[py * protoW + px] > 0.5f ? (byte)255 : (byte)0;
                            pixels[y * maskW + x] = new Color32(r, g, b, alpha);
                        }
                        else
                        {
                            pixels[y * maskW + x] = new Color32(r, g, b, 0);
                        }
                    }
                }
                
                texture.SetPixels32(pixels);
                texture.Apply();
                
                return texture;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ [YOLO26Seg] Ошибка генерации маски: {e.Message}");
                return null;
            }
        }
        
        private float Sigmoid(float x) => 1f / (1f + Mathf.Exp(-x));
        
        private Color GetObjectColor(int objectId)
        {
            // Генерируем уникальный цвет для каждого объекта (как в SimpleGUIVisualizer)
            float hue = (objectId * 0.618033988749895f) % 1.0f; // Golden ratio
            float saturation = 0.7f + (objectId % 3) * 0.1f;
            float value = 0.8f + (objectId % 2) * 0.15f;
            
            return Color.HSVToRGB(hue, saturation, value);
        }
        
        private void CleanupOldMasks()
        {
            if (lastResult == null || lastResult.masks == null) return;
            
            foreach (var mask in lastResult.masks)
            {
                if (mask.mask != null)
                {
                    UnityEngine.Object.Destroy(mask.mask);
                    mask.mask = null;
                }
            }
        }
        
        public override void Dispose()
        {
            CleanupOldMasks();
            base.Dispose();
        }
        
        private InferenceResult CreateFailedResult() =>
            new InferenceResult { success = false, modelType = ModelType.Segmentation };
        
        #endregion
    }
}
