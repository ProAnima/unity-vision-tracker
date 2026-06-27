using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UniversalTracker.Core;
using UniversalTracker.Processing;

namespace UniversalTracker.Tracking
{
    /// <summary>
    /// Простой трекер на основе IoU (Intersection over Union)
    /// </summary>
    public class IOUTracker : ITracker
    {
        #region Public Fields
        
        public float iouThreshold = 0.3f;
        public int maxMissedFrames = 10;
        
        #endregion
        
        #region Public Methods
        
        public IOUTracker(float iouThreshold = 0.3f, int maxMissedFrames = 10)
        {
            this.iouThreshold = iouThreshold;
            this.maxMissedFrames = maxMissedFrames;
            nmsProcessor = new NMSProcessor();
        }
        
        public TrackedObject[] Update(BBoxData[] detections, float deltaTime)
        {
            if (detections == null || detections.Length == 0)
            {
                UpdateMissedFrames();
                return trackedObjects.Values.Where(t => t.isActive).ToArray();
            }
            
            var unmatched = new List<BBoxData>(detections);
            var matched = new HashSet<int>();
            
            foreach (var tracked in trackedObjects.Values.Where(t => t.isActive))
            {
                float bestIoU = 0f;
                BBoxData bestMatch = null;
                
                foreach (var detection in unmatched)
                {
                    float iou = nmsProcessor.CalculateIoU(tracked.currentDetection.rect, detection.rect);
                    if (iou > bestIoU && iou > iouThreshold)
                    {
                        bestIoU = iou;
                        bestMatch = detection;
                    }
                }
                
                if (bestMatch != null)
                {
                    UpdateTrackedObject(tracked, bestMatch, deltaTime);
                    unmatched.Remove(bestMatch);
                    matched.Add(tracked.id);
                }
                else
                {
                    tracked.missedFrames++;
                    if (tracked.missedFrames > maxMissedFrames)
                        tracked.isActive = false;
                }
            }
            
            foreach (var detection in unmatched)
                CreateNewTrackedObject(detection);
            
            return trackedObjects.Values.Where(t => t.isActive).ToArray();
        }
        
        public void Reset()
        {
            trackedObjects.Clear();
            nextId = 0;
        }
        
        public TrackedObject GetTrackedObject(int id) =>
            trackedObjects.ContainsKey(id) ? trackedObjects[id] : null;
        
        public TrackedObject[] GetAllTrackedObjects() =>
            trackedObjects.Values.Where(t => t.isActive).ToArray();
        
        #endregion
        
        #region Private Fields
        
        private Dictionary<int, TrackedObject> trackedObjects = new Dictionary<int, TrackedObject>();
        private int nextId = 0;
        private NMSProcessor nmsProcessor;
        
        #endregion
        
        #region Private Methods
        
        private void UpdateTrackedObject(TrackedObject tracked, BBoxData detection, float deltaTime)
        {
            var prevCenter = tracked.currentDetection.center;
            tracked.currentDetection = detection;
            tracked.confidence = detection.confidence;
            tracked.missedFrames = 0;
            tracked.age++;
            
            if (deltaTime > 0)
                tracked.velocity = (detection.center - prevCenter) / deltaTime;
            
            tracked.predictedPosition = detection.center + tracked.velocity * deltaTime;
        }
        
        private void CreateNewTrackedObject(BBoxData detection)
        {
            var tracked = new TrackedObject(nextId++, detection);
            trackedObjects[tracked.id] = tracked;
        }
        
        private void UpdateMissedFrames()
        {
            foreach (var tracked in trackedObjects.Values.Where(t => t.isActive))
            {
                tracked.missedFrames++;
                if (tracked.missedFrames > maxMissedFrames)
                    tracked.isActive = false;
            }
        }
        
        #endregion
    }
}
