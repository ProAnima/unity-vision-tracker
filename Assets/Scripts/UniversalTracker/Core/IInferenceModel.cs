using UnityEngine;
using System;
using Unity.InferenceEngine;

namespace UniversalTracker.Core
{
    /// <summary>
    /// Базовый интерфейс для всех моделей нейросетей
    /// </summary>
    public interface IInferenceModel
    {
        ModelType ModelType { get; }
        bool IsInitialized { get; }
        
        void Initialize(ModelConfig config);
        InferenceResult RunInference(Texture inputTexture);
        void Dispose();
    }

    /// <summary>
    /// Конфигурация модели
    /// </summary>
    [Serializable]
    public class ModelConfig
    {
        public string modelName;
        public ModelAsset modelAsset;
        public BackendType backend = BackendType.GPUPixel;
        public int inputSize = 640; // YOLO11/26 требует 640! Не меняй на 320!
        
        [Range(0.1f, 0.9f)] public float confidenceThreshold = 0.5f;
        [Range(0.1f, 0.9f)] public float nmsThreshold = 0.45f;
        
        public bool flipX = false;
        public bool flipY = false; // Отключен! Visualizer рисует напрямую
        public Vector2 scale = Vector2.one;
        public Vector2 offset = Vector2.zero;
    }
}
