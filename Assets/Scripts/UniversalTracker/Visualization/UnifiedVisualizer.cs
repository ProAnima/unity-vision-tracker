using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UniversalTracker.Core;

namespace UniversalTracker.Visualization
{
    /// <summary>
    /// Универсальный визуализатор для всех типов детекций
    /// </summary>
    public class UnifiedVisualizer
    {
        #region Public Fields
        
        public Color bboxColor = Color.green;
        public Color keypointColor = Color.cyan;
        public Color connectionColor = Color.yellow;
        public float lineThickness = 2f;
        public bool showLabels = true;
        public float keypointRadius = 4f;
        public float keypointConfidenceThreshold = 0.5f;
        
        #endregion
        
        #region Public Methods
        
        public void Initialize(RectTransform root)
        {
            this.root = root;
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            
            if (font == null)
                Debug.LogWarning("⚠️ [UnifiedVisualizer] Не найден встроенный шрифт LegacyRuntime.ttf, подписи могут не отображаться");
        }
        
        public void Clear()
        {
            DeactivateAll(activeBoxes, boxPool);
            DeactivateAll(activeLines, linePool);
            DeactivateAll(activePoints, pointPool);
            DeactivateAll(activeLabels, labelPool);
        }
        
        public void DrawBoundingBoxes(BBoxData[] boxes, RectTransform targetRect, bool drawConfidence)
        {
            if (root == null)
                return;
            
            if (boxes == null || boxes.Length == 0)
                return;
            
            if (targetRect == null)
            {
                Debug.LogWarning("⚠️ [UnifiedVisualizer] targetRect отсутствует, bbox не рисуем");
                return;
            }
            
            var size = targetRect.rect.size;
            for (int i = 0; i < boxes.Length; i++)
                DrawBox(boxes[i], size, drawConfidence);
        }
        
        public void DrawKeypoints(KeypointData[] keypoints, RectTransform targetRect)
        {
            if (root == null)
                return;
            
            if (keypoints == null || keypoints.Length == 0)
                return;
            
            if (targetRect == null)
            {
                Debug.LogWarning("⚠️ [UnifiedVisualizer] targetRect отсутствует, keypoints не рисуем");
                return;
            }
            
            var size = targetRect.rect.size;
            for (int i = 0; i < keypoints.Length; i++)
                DrawSkeleton(keypoints[i], size);
        }
        
        public void DrawMasks(MaskData[] masks, RectTransform targetRect)
        {
            if (masks == null || masks.Length == 0)
                return;
            
            Debug.LogWarning("⚠️ [UnifiedVisualizer] Маски пока не поддержаны в UI визуализации");
        }
        
        #endregion
        
        #region Private Methods
        
        private void DrawBox(BBoxData box, Vector2 size, bool drawConfidence)
        {
            var r = NormalizedToRect(box.rect, size);
            
            if (Time.frameCount % 60 == 0)
                Debug.Log($"🎨 DrawBox: norm=({box.rect.x:F3}, {box.rect.y:F3}, {box.rect.width:F3}, {box.rect.height:F3}) → px=({r.x:F0}, {r.y:F0}, {r.width:F0}, {r.height:F0}) canvasSize={size}");
            
            if (r.width <= 1f || r.height <= 1f)
            {
                if (Time.frameCount % 60 == 0)
                    Debug.LogWarning($"⚠️ DrawBox: bbox слишком маленький!");
                return;
            }
            
            var container = RentBox();
            var rt = container.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(r.x, r.y);
            rt.sizeDelta = new Vector2(r.width, r.height);
            
            ConfigureBoxLines(container, r.size);
            
            if (showLabels)
                DrawLabel(box, drawConfidence, r, size);
        }
        
        private void DrawSkeleton(KeypointData kp, Vector2 size)
        {
            if (kp.points == null || kp.points.Length == 0)
                return;
            
            if (Time.frameCount % 60 == 0)
                Debug.Log($"🎨 DrawSkeleton: {kp.points.Length} keypoints, size={size}");
            
            int visiblePoints = 0;
            for (int i = 0; i < kp.points.Length; i++)
            {
                var c = GetConfidence(kp, i);
                if (c >= keypointConfidenceThreshold)
                {
                    DrawPoint(kp, i, size);
                    visiblePoints++;
                }
            }
            
            if (Time.frameCount % 60 == 0)
                Debug.Log($"   → Нарисовано {visiblePoints} точек");
            
            if (kp.connections == null || kp.connections.Length < 2)
                return;
            
            int drawnConnections = 0;
            for (int i = 0; i < kp.connections.Length; i += 2)
            {
                int idx1 = kp.connections[i];
                int idx2 = kp.connections[i + 1];
                
                if (idx1 < 0 || idx2 < 0) continue;
                if (idx1 >= kp.points.Length || idx2 >= kp.points.Length) continue;
                
                var c1 = GetConfidence(kp, idx1);
                var c2 = GetConfidence(kp, idx2);
                if (c1 < keypointConfidenceThreshold || c2 < keypointConfidenceThreshold) continue;
                
                var p1 = NormalizedToPoint(kp.points[idx1], size);
                var p2 = NormalizedToPoint(kp.points[idx2], size);
                DrawLine(p1, p2);
                drawnConnections++;
            }
            
            if (Time.frameCount % 60 == 0)
                Debug.Log($"   → Нарисовано {drawnConnections} связей");
        }
        
        private void DrawPoint(KeypointData kp, int index, Vector2 size)
        {
            var c = GetConfidence(kp, index);
            if (c < keypointConfidenceThreshold)
                return;
            
            var p = NormalizedToPoint(kp.points[index], size);
            
            // Debug первых 3 keypoints раз в 60 кадров
            if (Time.frameCount % 60 == 0 && index < 3)
                Debug.Log($"   DrawPoint[{index}]: norm=({kp.points[index].x:F3}, {kp.points[index].y:F3}) → px=({p.x:F0}, {p.y:F0}) conf={c:F2}");
            
            var go = RentPoint();
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(p.x - keypointRadius, p.y - keypointRadius);
            rt.sizeDelta = new Vector2(keypointRadius * 2f, keypointRadius * 2f);
        }
        
        private void DrawLine(Vector2 p1, Vector2 p2)
        {
            var delta = p2 - p1;
            var len = delta.magnitude;
            if (len <= 1f)
                return;
            
            var mid = (p1 + p2) * 0.5f;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            
            var go = RentLine();
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(mid.x, mid.y);
            rt.sizeDelta = new Vector2(len, lineThickness);
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
        
        private void DrawLabel(BBoxData box, bool drawConfidence, Rect r, Vector2 size)
        {
            if (font == null)
                return;
            
            var go = RentLabel();
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(r.x, r.y + r.height + 2f);
            rt.sizeDelta = new Vector2(Mathf.Min(400f, size.x), 20f);
            
            var text = go.GetComponent<Text>();
            text.color = bboxColor;
            text.font = font;
            text.fontSize = 14;
            text.alignment = TextAnchor.LowerLeft;
            
            var conf = drawConfidence ? $" {box.confidence:F2}" : "";
            var id = box.id != 0 ? $" #{box.id}" : "";
            text.text = $"{box.className}{conf}{id}";
        }
        
        private float GetConfidence(KeypointData kp, int index)
        {
            if (kp.confidences == null)
                return 1f;
            
            if (index < 0 || index >= kp.confidences.Length)
                return 1f;
            
            return kp.confidences[index];
        }
        
        private Rect NormalizedToRect(Rect normalized, Vector2 size)
        {
            // Простая конвертация без flip (YOLO coords → UI coords напрямую)
            float x = normalized.x * size.x;
            float y = normalized.y * size.y;
            float w = normalized.width * size.x;
            float h = normalized.height * size.y;
            
            return new Rect(x, y, w, h);
        }
        
        private Vector2 NormalizedToPoint(Vector2 normalized, Vector2 size)
        {
            // Простая конвертация без flip
            float x = normalized.x * size.x;
            float y = normalized.y * size.y;
            
            return new Vector2(x, y);
        }
        
        private GameObject RentBox() => Rent(boxPool, activeBoxes, "UT_Box");
        private GameObject RentLine() => Rent(linePool, activeLines, "UT_Line");
        private GameObject RentPoint() => Rent(pointPool, activePoints, "UT_Point");
        private GameObject RentLabel() => Rent(labelPool, activeLabels, "UT_Label");
        
        private GameObject Rent(List<GameObject> pool, List<GameObject> active, string name)
        {
            GameObject go;
            if (pool.Count > 0)
            {
                int last = pool.Count - 1;
                go = pool[last];
                pool.RemoveAt(last);
            }
            else
            {
                go = Create(name);
            }
            
            go.SetActive(true);
            active.Add(go);
            return go;
        }
        
        private GameObject Create(string name)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(root, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.localScale = Vector3.one;
            
            if (name == "UT_Box")
                CreateBoxVisual(go);
            else if (name == "UT_Line")
                CreateLineVisual(go);
            else if (name == "UT_Point")
                CreatePointVisual(go);
            else if (name == "UT_Label")
                CreateLabelVisual(go);
            
            return go;
        }
        
        private void CreateBoxVisual(GameObject container)
        {
            CreateLine(container.transform, "L", new Vector2(0f, 0f), new Vector2(0f, 1f), bboxColor);
            CreateLine(container.transform, "R", new Vector2(1f, 0f), new Vector2(1f, 1f), bboxColor);
            CreateLine(container.transform, "B", new Vector2(0f, 0f), new Vector2(1f, 0f), bboxColor);
            CreateLine(container.transform, "T", new Vector2(0f, 1f), new Vector2(1f, 1f), bboxColor);
        }
        
        private void ConfigureBoxLines(GameObject container, Vector2 size)
        {
            var t = container.transform;
            var l = t.Find("L")?.GetComponent<RectTransform>();
            var r = t.Find("R")?.GetComponent<RectTransform>();
            var b = t.Find("B")?.GetComponent<RectTransform>();
            var top = t.Find("T")?.GetComponent<RectTransform>();
            
            if (l != null) l.sizeDelta = new Vector2(lineThickness, size.y);
            if (r != null) r.sizeDelta = new Vector2(lineThickness, size.y);
            if (b != null) b.sizeDelta = new Vector2(size.x, lineThickness);
            if (top != null) top.sizeDelta = new Vector2(size.x, lineThickness);
        }
        
        private void CreateLineVisual(GameObject go)
        {
            var img = go.AddComponent<Image>();
            img.color = connectionColor;
            var rt = go.GetComponent<RectTransform>();
            rt.pivot = new Vector2(0.5f, 0.5f);
        }
        
        private void CreatePointVisual(GameObject go)
        {
            var img = go.AddComponent<Image>();
            img.color = keypointColor;
        }
        
        private void CreateLabelVisual(GameObject go)
        {
            go.AddComponent<Text>();
        }
        
        private void CreateLine(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(anchorMin.x, anchorMin.y);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            var img = go.AddComponent<Image>();
            img.color = color;
        }
        
        private void DeactivateAll(List<GameObject> active, List<GameObject> pool)
        {
            for (int i = 0; i < active.Count; i++)
            {
                var go = active[i];
                if (go == null) continue;
                go.SetActive(false);
                pool.Add(go);
            }
            active.Clear();
        }
        
        #endregion
        
        #region Private Fields
        
        private RectTransform root;
        private Font font;
        
        private readonly List<GameObject> boxPool = new List<GameObject>();
        private readonly List<GameObject> linePool = new List<GameObject>();
        private readonly List<GameObject> pointPool = new List<GameObject>();
        private readonly List<GameObject> labelPool = new List<GameObject>();
        
        private readonly List<GameObject> activeBoxes = new List<GameObject>();
        private readonly List<GameObject> activeLines = new List<GameObject>();
        private readonly List<GameObject> activePoints = new List<GameObject>();
        private readonly List<GameObject> activeLabels = new List<GameObject>();
        
        #endregion
    }
}
