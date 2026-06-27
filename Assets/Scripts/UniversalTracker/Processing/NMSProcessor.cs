using UnityEngine;
using System.Collections.Generic;
using UniversalTracker.Core;

namespace UniversalTracker.Processing
{
    /// <summary>
    /// Non-Maximum Suppression (NMS) для фильтрации перекрывающихся детекций
    /// </summary>
    public class NMSProcessor
    {
        #region Public Methods
        
        public BBoxData[] ApplyNMS(BBoxData[] boxes, float iouThreshold)
        {
            if (boxes == null || boxes.Length == 0)
                return boxes;
            
            var boxList = new List<BBoxData>(boxes);
            boxList.Sort((a, b) => b.confidence.CompareTo(a.confidence));
            
            var results = new List<BBoxData>();
            var active = new bool[boxList.Count];
            
            for (int i = 0; i < active.Length; i++)
                active[i] = true;
            
            for (int i = 0; i < boxList.Count; i++)
            {
                if (!active[i]) continue;
                
                results.Add(boxList[i]);
                
                for (int j = i + 1; j < boxList.Count; j++)
                {
                    if (!active[j]) continue;
                    
                    float iou = CalculateIoU(boxList[i].rect, boxList[j].rect);
                    if (iou > iouThreshold)
                        active[j] = false;
                }
            }
            
            return results.ToArray();
        }
        
        public BBoxData[] ApplyClassAgnosticNMS(BBoxData[] boxes, float iouThreshold)
        {
            return ApplyNMS(boxes, iouThreshold);
        }
        
        public BBoxData[] ApplyPerClassNMS(BBoxData[] boxes, float iouThreshold)
        {
            if (boxes == null || boxes.Length == 0)
                return boxes;
            
            var resultList = new List<BBoxData>();
            var classGroups = new Dictionary<int, List<BBoxData>>();
            
            foreach (var box in boxes)
            {
                if (!classGroups.ContainsKey(box.classId))
                    classGroups[box.classId] = new List<BBoxData>();
                
                classGroups[box.classId].Add(box);
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
        
        #endregion
    }
}
