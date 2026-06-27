using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UniversalTracker.Core;
using UniversalTracker.Processing;

namespace UniversalTracker.Tracking
{
    /// <summary>
    /// SORT (Simple Online and Realtime Tracking) трекер с Kalman фильтром
    /// </summary>
    public class SORTTracker : ITracker
    {
        #region Public Fields
        
        public float iouThreshold = 0.3f;
        public int maxMissedFrames = 5;
        public int minHitsToConfirm = 3;
        
        #endregion
        
        #region Public Methods
        
        public SORTTracker(float iouThreshold = 0.3f, int maxMissedFrames = 5)
        {
            this.iouThreshold = iouThreshold;
            this.maxMissedFrames = maxMissedFrames;
            nmsProcessor = new NMSProcessor();
        }
        
        public TrackedObject[] Update(BBoxData[] detections, float deltaTime)
        {
            if (detections == null || detections.Length == 0)
            {
                UpdatePredictions(deltaTime);
                CleanupOldTracks();
                return GetConfirmedTracks();
            }
            
            UpdatePredictions(deltaTime);
            
            var matches = AssociateDetectionsToTracks(detections);
            
            UpdateMatchedTracks(matches, deltaTime);
            CreateNewTracks(detections, matches);
            CleanupOldTracks();
            
            return GetConfirmedTracks();
        }
        
        public void Reset()
        {
            trackedObjects.Clear();
            nextId = 0;
        }
        
        public TrackedObject GetTrackedObject(int id) =>
            trackedObjects.ContainsKey(id) ? trackedObjects[id] : null;
        
        public TrackedObject[] GetAllTrackedObjects() =>
            trackedObjects.Values.Where(t => t.isActive && t.age >= minHitsToConfirm).ToArray();
        
        #endregion
        
        #region Private Fields
        
        private Dictionary<int, TrackedObject> trackedObjects = new Dictionary<int, TrackedObject>();
        private int nextId = 0;
        private NMSProcessor nmsProcessor;
        
        #endregion
        
        #region Private Methods
        
        private void UpdatePredictions(float deltaTime)
        {
            foreach (var tracked in trackedObjects.Values.Where(t => t.isActive))
            {
                // Простое предсказание на основе скорости (упрощенный Kalman)
                if (deltaTime > 0)
                    tracked.predictedPosition = tracked.currentDetection.center + tracked.velocity * deltaTime;
            }
        }
        
        private Dictionary<int, BBoxData> AssociateDetectionsToTracks(BBoxData[] detections)
        {
            var matches = new Dictionary<int, BBoxData>();
            var usedDetections = new HashSet<int>();
            
            var activeTracks = trackedObjects.Values.Where(t => t.isActive).OrderByDescending(t => t.confidence).ToList();
            
            foreach (var tracked in activeTracks)
            {
                float bestIoU = 0f;
                int bestIdx = -1;
                
                for (int i = 0; i < detections.Length; i++)
                {
                    if (usedDetections.Contains(i)) continue;
                    
                    float iou = nmsProcessor.CalculateIoU(tracked.currentDetection.rect, detections[i].rect);
                    if (iou > bestIoU && iou > iouThreshold)
                    {
                        bestIoU = iou;
                        bestIdx = i;
                    }
                }
                
                if (bestIdx >= 0)
                {
                    matches[tracked.id] = detections[bestIdx];
                    usedDetections.Add(bestIdx);
                }
            }
            
            return matches;
        }
        
        private void UpdateMatchedTracks(Dictionary<int, BBoxData> matches, float deltaTime)
        {
            foreach (var tracked in trackedObjects.Values.Where(t => t.isActive))
            {
                if (matches.ContainsKey(tracked.id))
                {
                    var detection = matches[tracked.id];
                    var prevCenter = tracked.currentDetection.center;
                    
                    tracked.currentDetection = detection;
                    tracked.confidence = detection.confidence;
                    tracked.missedFrames = 0;
                    tracked.age++;
                    
                    if (deltaTime > 0)
                    {
                        var newVelocity = (detection.center - prevCenter) / deltaTime;
                        tracked.velocity = Vector2.Lerp(tracked.velocity, newVelocity, 0.5f);
                    }
                }
                else
                {
                    tracked.missedFrames++;
                }
            }
        }
        
        private void CreateNewTracks(BBoxData[] detections, Dictionary<int, BBoxData> matches)
        {
            var usedDetections = new HashSet<BBoxData>(matches.Values);
            
            foreach (var detection in detections)
            {
                if (!usedDetections.Contains(detection))
                {
                    var tracked = new TrackedObject(nextId++, detection);
                    trackedObjects[tracked.id] = tracked;
                }
            }
        }
        
        private void CleanupOldTracks()
        {
            var toRemove = trackedObjects.Values.Where(t => t.missedFrames > maxMissedFrames).Select(t => t.id).ToList();
            
            foreach (var id in toRemove)
                trackedObjects.Remove(id);
        }
        
        private TrackedObject[] GetConfirmedTracks() =>
            trackedObjects.Values.Where(t => t.isActive && t.age >= minHitsToConfirm).ToArray();
        
        #endregion
    }
}
