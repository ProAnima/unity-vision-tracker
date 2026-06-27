using UnityEngine;
using System;

namespace UniversalTracker.Core
{
    /// <summary>
    /// Универсальный результат inference любой модели
    /// </summary>
    [Serializable]
    public class InferenceResult
    {
        public bool success;
        public ModelType modelType;
        public float inferenceTime;
        public int detectionCount;
        
        public BBoxData[] boundingBoxes;
        public KeypointData[] keypoints;
        public MaskData[] masks;
        public ClassificationData[] classifications;
        
        public float maxConfidence;
        public Texture2D debugTexture;
    }

    /// <summary>
    /// Данные bounding box
    /// </summary>
    [Serializable]
    public class BBoxData
    {
        public int id;
        public int classId;
        public string className;
        public float confidence;
        public Rect rect;
        public Vector2 center;
        public float angle;
    }

    /// <summary>
    /// Данные ключевых точек (скелет)
    /// </summary>
    [Serializable]
    public class KeypointData
    {
        public int personId;
        public Vector2[] points;
        public float[] confidences;
        public int[] connections;
    }

    /// <summary>
    /// Данные маски сегментации
    /// </summary>
    [Serializable]
    public class MaskData
    {
        public int objectId;
        public int classId;
        public string className;
        public float confidence;
        public Rect bbox; // Добавлено для упрощённой визуализации
        public Texture2D mask; // Полная маска (если восстановлена)
        public float[] maskCoefficients; // Коэффициенты для proto-маски (YOLO seg)
        public Vector2[] contour;
    }

    /// <summary>
    /// Данные классификации
    /// </summary>
    [Serializable]
    public class ClassificationData
    {
        public int classId;
        public string className;
        public float confidence;
    }
}
