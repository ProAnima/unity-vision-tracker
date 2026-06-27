using UniversalTracker.Core;
using UnityEngine;

namespace UniversalTracker.Tracking
{
    /// <summary>
    /// Интерфейс трекера объектов между кадрами
    /// </summary>
    public interface ITracker
    {
        TrackedObject[] Update(VisionDetection[] detections, float deltaTime);
        void Reset();
        TrackedObject GetTrackedObject(int id);
        TrackedObject[] GetAllTrackedObjects();
    }

    /// <summary>
    /// Отслеживаемый объект с историей
    /// </summary>
    public class TrackedObject
    {
        public int id;
        public int classId;
        public string label;
        public VisionDetection currentDetection;
        public Vector2 velocity;
        public Vector2 predictedPosition;
        public float confidence;
        public int age;
        public int missedFrames;
        public bool isActive;
        
        public TrackedObject(int id, VisionDetection detection)
        {
            this.id = id;
            this.classId = detection.classId;
            this.label = detection.label;
            this.currentDetection = detection;
            this.velocity = Vector2.zero;
            this.predictedPosition = DetectionCenter(detection);
            this.confidence = detection.confidence;
            this.age = 0;
            this.missedFrames = 0;
            this.isActive = true;
        }

        public static Vector2 DetectionCenter(VisionDetection detection)
        {
            if (detection.sourceCenter != Vector2.zero)
                return detection.sourceCenter;

            Rect rect = detection.sourceRect.width > 0f && detection.sourceRect.height > 0f
                ? detection.sourceRect
                : detection.normalizedRect;

            return rect.center;
        }
    }
}
