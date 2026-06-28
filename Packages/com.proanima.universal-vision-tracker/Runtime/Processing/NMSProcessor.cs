using System.Collections.Generic;
using UnityEngine;
using UniversalTracker.Core;

namespace UniversalTracker.Processing
{
    /// <summary>
    /// Non-Maximum Suppression (NMS) для фильтрации перекрывающихся детекций
    /// </summary>
    public class NMSProcessor
    {
        #region Public Methods
        
        public VisionDetection[] ApplyNMS(VisionDetection[] detections, float iouThreshold)
        {
            if (detections == null || detections.Length == 0)
                return detections;
            
            var detectionList = new List<VisionDetection>(detections);
            detectionList.Sort((a, b) => b.confidence.CompareTo(a.confidence));
            
            var results = new List<VisionDetection>();
            var active = new bool[detectionList.Count];
            
            for (int i = 0; i < active.Length; i++)
                active[i] = true;
            
            for (int i = 0; i < detectionList.Count; i++)
            {
                if (!active[i]) continue;
                
                results.Add(detectionList[i]);
                
                for (int j = i + 1; j < detectionList.Count; j++)
                {
                    if (!active[j]) continue;
                    
                    float iou = CalculateIoU(GetComparisonRect(detectionList[i]), GetComparisonRect(detectionList[j]));
                    if (iou > iouThreshold)
                        active[j] = false;
                }
            }
            
            return results.ToArray();
        }
        
        public VisionDetection[] ApplyClassAgnosticNMS(VisionDetection[] detections, float iouThreshold)
        {
            return ApplyNMS(detections, iouThreshold);
        }
        
        public VisionDetection[] ApplyPerClassNMS(VisionDetection[] detections, float iouThreshold)
        {
            if (detections == null || detections.Length == 0)
                return detections;
            
            var resultList = new List<VisionDetection>();
            var classGroups = new Dictionary<int, List<VisionDetection>>();
            
            foreach (var detection in detections)
            {
                if (!classGroups.ContainsKey(detection.classId))
                    classGroups[detection.classId] = new List<VisionDetection>();
                
                classGroups[detection.classId].Add(detection);
            }
            
            foreach (var group in classGroups.Values)
            {
                var nmsResults = ApplyNMS(group.ToArray(), iouThreshold);
                resultList.AddRange(nmsResults);
            }
            
            return resultList.ToArray();
        }
        
        public float CalculateIoU(Rect a, Rect b)
        {
            float x1 = Mathf.Max(a.xMin, b.xMin);
            float y1 = Mathf.Max(a.yMin, b.yMin);
            float x2 = Mathf.Min(a.xMax, b.xMax);
            float y2 = Mathf.Min(a.yMax, b.yMax);
            
            if (x2 < x1 || y2 < y1)
                return 0f;
            
            float intersection = (x2 - x1) * (y2 - y1);
            float area1 = a.width * a.height;
            float area2 = b.width * b.height;
            
            return intersection / (area1 + area2 - intersection);
        }

        public Rect GetComparisonRect(VisionDetection detection)
        {
            return detection.normalizedRect.width > 0f && detection.normalizedRect.height > 0f
                ? detection.normalizedRect
                : detection.sourceRect;
        }
        
        #endregion
    }
}
