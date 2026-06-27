using UnityEngine;
using UniversalTracker.Core;

namespace UniversalTracker.Tracking
{
    /// <summary>
    /// Интерфейс трекера объектов между кадрами
    /// </summary>
    public interface ITracker
    {
        TrackedObject[] Update(BBoxData[] detections, float deltaTime);
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
        public string className;
        public BBoxData currentDetection;
        public Vector2 velocity;
        public Vector2 predictedPosition;
        public float confidence;
        public int age;
        public int missedFrames;
        public bool isActive;
        
        public TrackedObject(int id, BBoxData detection)
        {
            this.id = id;
            this.classId = detection.classId;
            this.className = detection.className;
            this.currentDetection = detection;
            this.velocity = Vector2.zero;
            this.predictedPosition = detection.center;
            this.confidence = detection.confidence;
            this.age = 0;
            this.missedFrames = 0;
            this.isActive = true;
        }
    }
}
