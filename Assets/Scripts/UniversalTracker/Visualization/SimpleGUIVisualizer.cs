using UnityEngine;
using UniversalTracker.Core;

namespace UniversalTracker.Visualization
{
    /// <summary>
    /// Простой визуализатор через OnGUI (как в официальных примерах)
    /// </summary>
    public class SimpleGUIVisualizer : MonoBehaviour
    {
        #region Public Fields
        
        [Header("🎨 Настройки")]
        public new bool enabled = true;
        public Color bboxColor = Color.green;
        public Color keypointColor = Color.cyan;
        public Color connectionColor = Color.yellow;
        public float lineThickness = 3f;
        public float pointRadius = 5f;
        public int fontSize = 14;
        
        [Header("🔧 Отладка")]
        public bool showDebugInfo = true;
        public bool drawBoundingBoxes = true;
        public bool drawKeypoints = true;
        public bool drawConnections = true;
        public bool drawMasks = false;
        public float maskAlpha = 0.4f;
        
        #endregion
        
        #region Private Fields
        
        private InferenceResult lastResult;
        private Texture lastTexture;
        private Texture2D whiteTexture;
        private GUIStyle labelStyle;
        
        #endregion
        
        #region Public Methods
        
        public void UpdateVisualization(InferenceResult result, Texture sourceTexture)
        {
            // НЕ удаляем текстуры каждый кадр - они управляются моделью!
            // Удаление происходит только при OnDestroy
            lastResult = result;
            lastTexture = sourceTexture;
        }
        
        void OnDestroy()
        {
            // Очистка всех текстур масок при уничтожении компонента
            if (lastResult != null && lastResult.masks != null)
            {
                foreach (var mask in lastResult.masks)
                {
                    if (mask.mask != null)
                        Destroy(mask.mask);
                }
            }
            
            if (whiteTexture != null)
                Destroy(whiteTexture);
        }
        
        #endregion
        
        #region Unity Callbacks
        
        void Start()
        {
            // Создаём белую текстуру для линий
            whiteTexture = new Texture2D(1, 1);
            whiteTexture.SetPixel(0, 0, Color.white);
            whiteTexture.Apply();
            
            labelStyle = new GUIStyle();
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontSize = fontSize;
        }
        
        void OnGUI()
        {
            if (!enabled || lastResult == null || lastTexture == null)
                return;
            
            // Размеры экрана
            float screenW = Screen.width;
            float screenH = Screen.height;
            
            // Debug раз в 60 кадров
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"🎨 [SimpleGUI] OnGUI: BBoxes={lastResult.boundingBoxes?.Length ?? 0}, Keypoints={lastResult.keypoints?.Length ?? 0}");
                
                if (lastResult.keypoints != null && lastResult.keypoints.Length > 0)
                {
                    var kp = lastResult.keypoints[0];
                    Debug.Log($"   KP[0]: points={kp.points?.Length ?? 0}, connections={kp.connections?.Length ?? 0}");
                    
                    if (kp.points != null && kp.points.Length > 0)
                    {
                        Debug.Log($"   KP[0].point[0]: ({kp.points[0].x:F3}, {kp.points[0].y:F3}) conf={kp.confidences?[0] ?? -1:F2}");
                    }
                }
            }
            
            // Рисуем bbox
            if (drawBoundingBoxes && lastResult.boundingBoxes != null)
            {
                foreach (var box in lastResult.boundingBoxes)
                {
                    DrawBBox(box, screenW, screenH);
                }
            }
            
            // Рисуем masks (полупрозрачные заливки)
            if (drawMasks && lastResult.masks != null)
            {
                foreach (var mask in lastResult.masks)
                {
                    DrawMask(mask, screenW, screenH);
                }
            }
            
            // Рисуем keypoints
            if (lastResult.keypoints != null)
            {
                foreach (var kp in lastResult.keypoints)
                {
                    if (drawConnections)
                        DrawConnections(kp, screenW, screenH);
                    
                    if (drawKeypoints)
                        DrawPoints(kp, screenW, screenH);
                }
            }
            
            // Debug info
            if (showDebugInfo)
            {
                DrawDebugInfo();
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void DrawBBox(BBoxData box, float screenW, float screenH)
        {
            // Normalized (0-1) → Screen pixels
            // YOLO: (x,y) = top-left corner, (w,h) = size
            float x = box.rect.x * screenW;
            float y = box.rect.y * screenH;
            float w = box.rect.width * screenW;
            float h = box.rect.height * screenH;
            
            Rect screenRect = new Rect(x, y, w, h);
            
            GUI.color = bboxColor;
            
            // Верхняя линия
            GUI.DrawTexture(new Rect(x, y, w, lineThickness), whiteTexture);
            // Нижняя линия
            GUI.DrawTexture(new Rect(x, y + h - lineThickness, w, lineThickness), whiteTexture);
            // Левая линия
            GUI.DrawTexture(new Rect(x, y, lineThickness, h), whiteTexture);
            // Правая линия
            GUI.DrawTexture(new Rect(x + w - lineThickness, y, lineThickness, h), whiteTexture);
            
            // Label
            GUI.color = Color.white;
            string label = $"{box.className} {box.confidence:F2}";
            GUI.Label(new Rect(x + 5, y - 20, 200, 20), label, labelStyle);
        }
        
        private void DrawPoints(KeypointData kp, float screenW, float screenH)
        {
            if (kp.points == null)
            {
                if (Time.frameCount % 60 == 0)
                    Debug.LogWarning("⚠️ [SimpleGUI] DrawPoints: points == null!");
                return;
            }
            
            GUI.color = keypointColor;
            
            int drawnPoints = 0;
            
            for (int i = 0; i < kp.points.Length; i++)
            {
                float conf = kp.confidences != null && i < kp.confidences.Length ? kp.confidences[i] : 1f;
                
                if (conf < 0.5f)
                    continue;
                
                float px = kp.points[i].x * screenW;
                float py = kp.points[i].y * screenH;
                
                float r = pointRadius;
                GUI.DrawTexture(new Rect(px - r, py - r, r * 2, r * 2), whiteTexture);
                drawnPoints++;
                
                // Debug первых 3 точек
                if (Time.frameCount % 60 == 0 && i < 3)
                    Debug.Log($"   DrawPoint[{i}]: norm=({kp.points[i].x:F3}, {kp.points[i].y:F3}) → screen=({px:F0}, {py:F0}) conf={conf:F2}");
            }
            
            if (Time.frameCount % 60 == 0)
                Debug.Log($"   → Нарисовано точек: {drawnPoints}/{kp.points.Length}");
        }
        
        private void DrawConnections(KeypointData kp, float screenW, float screenH)
        {
            if (kp.points == null || kp.connections == null)
            {
                if (Time.frameCount % 60 == 0)
                    Debug.LogWarning($"⚠️ [SimpleGUI] DrawConnections: points={kp.points == null}, connections={kp.connections == null}");
                return;
            }
            
            if (Time.frameCount % 60 == 0)
                Debug.Log($"   DrawConnections: {kp.connections.Length} connection indices");
            
            GUI.color = connectionColor;
            
            int drawnConnections = 0;
            
            for (int i = 0; i < kp.connections.Length; i += 2)
            {
                int idx1 = kp.connections[i];
                int idx2 = kp.connections[i + 1];
                
                if (idx1 < 0 || idx2 < 0 || idx1 >= kp.points.Length || idx2 >= kp.points.Length)
                    continue;
                
                float conf1 = kp.confidences != null && idx1 < kp.confidences.Length ? kp.confidences[idx1] : 1f;
                float conf2 = kp.confidences != null && idx2 < kp.confidences.Length ? kp.confidences[idx2] : 1f;
                
                if (conf1 < 0.5f || conf2 < 0.5f)
                    continue;
                
                Vector2 p1 = new Vector2(kp.points[idx1].x * screenW, kp.points[idx1].y * screenH);
                Vector2 p2 = new Vector2(kp.points[idx2].x * screenW, kp.points[idx2].y * screenH);
                
                DrawLine(p1, p2, lineThickness);
                drawnConnections++;
            }
            
            if (Time.frameCount % 60 == 0)
                Debug.Log($"   → Нарисовано связей: {drawnConnections}/{kp.connections.Length / 2}");
        }
        
        private void DrawLine(Vector2 p1, Vector2 p2, float thickness)
        {
            Vector2 diff = p2 - p1;
            float length = diff.magnitude;
            
            if (length < 1f)
                return;
            
            float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
            
            Vector2 center = (p1 + p2) * 0.5f;
            
            GUIUtility.RotateAroundPivot(angle, center);
            GUI.DrawTexture(new Rect(center.x - length * 0.5f, center.y - thickness * 0.5f, length, thickness), whiteTexture);
            GUIUtility.RotateAroundPivot(-angle, center);
        }
        
        private void DrawDebugInfo()
        {
            GUI.color = Color.yellow;
            GUIStyle debugStyle = new GUIStyle(labelStyle);
            debugStyle.fontSize = 16;
            
            int y = 10;
            GUI.Label(new Rect(10, y, 500, 20), $"Inference: {lastResult.inferenceTime:F1}ms", debugStyle);
            y += 20;
            GUI.Label(new Rect(10, y, 500, 20), $"Detections: {lastResult.detectionCount}", debugStyle);
            y += 20;
            GUI.Label(new Rect(10, y, 500, 20), $"Model: {lastResult.modelType}", debugStyle);
            y += 20;
            
            if (lastResult.boundingBoxes != null && lastResult.boundingBoxes.Length > 0)
            {
                var box = lastResult.boundingBoxes[0];
                GUI.Label(new Rect(10, y, 600, 20), $"BBox[0]: ({box.rect.x:F3}, {box.rect.y:F3}, {box.rect.width:F3}, {box.rect.height:F3})", debugStyle);
                y += 20;
            }
            
            if (lastResult.keypoints != null && lastResult.keypoints.Length > 0)
            {
                var kp = lastResult.keypoints[0];
                if (kp.points != null && kp.points.Length > 0)
                {
                    GUI.Label(new Rect(10, y, 600, 20), $"KP[0]: ({kp.points[0].x:F3}, {kp.points[0].y:F3}) conf={kp.confidences[0]:F2}", debugStyle);
                }
            }
        }
        
        private void DrawMask(MaskData mask, float screenW, float screenH)
        {
            float x = mask.bbox.x * screenW;
            float y = mask.bbox.y * screenH;
            float w = mask.bbox.width * screenW;
            float h = mask.bbox.height * screenH;
            
            // Если есть реальная маска - рисуем её
            if (mask.mask != null)
            {
                // Маска уже содержит цвет! Просто рисуем с альфой
                GUI.color = new Color(1f, 1f, 1f, maskAlpha); // Модулируем только альфу
                
                // Флип по Y: используем UV координаты (0,1) -> (1,0)
                Rect texCoords = new Rect(0, 1, 1, -1);
                GUI.DrawTextureWithTexCoords(new Rect(x, y, w, h), mask.mask, texCoords, true);
                
                // Контур вокруг bbox того же цвета что и маска
                Color maskColor = GetObjectColor(mask.objectId, mask.classId);
                GUI.color = new Color(maskColor.r, maskColor.g, maskColor.b, 1f);
                GUI.DrawTexture(new Rect(x, y, w, lineThickness), whiteTexture); // Top
                GUI.DrawTexture(new Rect(x, y + h - lineThickness, w, lineThickness), whiteTexture); // Bottom
                GUI.DrawTexture(new Rect(x, y, lineThickness, h), whiteTexture); // Left
                GUI.DrawTexture(new Rect(x + w - lineThickness, y, lineThickness, h), whiteTexture); // Right
            }
            else
            {
                // Fallback: рисуем полупрозрачный bbox (если маска не восстановлена)
                Color maskColor = GetObjectColor(mask.objectId, mask.classId);
                maskColor.a = maskAlpha * 0.3f;
                GUI.color = maskColor;
                GUI.DrawTexture(new Rect(x, y, w, h), whiteTexture);
            }
            
            // Label
            GUI.color = Color.white;
            string label = $"{mask.className} {mask.confidence:F2}";
            GUI.Label(new Rect(x + 5, y - 20, 200, 20), label, labelStyle);
        }
        
        private Color GetObjectColor(int objectId, int classId)
        {
            // Генерируем уникальный цвет для КАЖДОГО объекта
            // Используем HSV для красивых различимых цветов
            float hue = (objectId * 0.618033988749895f) % 1.0f; // Golden ratio для равномерного распределения
            float saturation = 0.7f + (objectId % 3) * 0.1f;    // Варьируем насыщенность
            float value = 0.8f + (objectId % 2) * 0.15f;        // Варьируем яркость
            
            Color color = Color.HSVToRGB(hue, saturation, value);
            return color;
        }
        
        private Color GetClassColor(int classId)
        {
            // Генерируем уникальный цвет для каждого класса (если нужно для bbox)
            Color[] colors = new Color[] {
                new Color(1f, 0f, 0f),      // Red
                new Color(0f, 1f, 0f),      // Green  
                new Color(0f, 0f, 1f),      // Blue
                new Color(1f, 1f, 0f),      // Yellow
                new Color(1f, 0f, 1f),      // Magenta
                new Color(0f, 1f, 1f),      // Cyan
                new Color(1f, 0.5f, 0f),    // Orange
                new Color(0.5f, 0f, 1f),    // Purple
                new Color(0f, 1f, 0.5f),    // Spring Green
                new Color(1f, 0f, 0.5f)     // Rose
            };
            
            return colors[classId % colors.Length];
        }
        
        #endregion
    }
}
