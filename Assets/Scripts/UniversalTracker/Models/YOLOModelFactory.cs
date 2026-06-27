using UnityEngine;
using UniversalTracker.Core;

namespace UniversalTracker.Models
{
    /// <summary>
    /// Фабрика для автоматического создания правильной модели YOLO по имени файла
    /// </summary>
    public static class YOLOModelFactory
    {
        #region Public Methods
        
        /// <summary>
        /// Создает модель автоматически на основе имени файла и типа задачи
        /// </summary>
        public static IInferenceModel CreateModel(ModelConfig config)
        {
            if (config?.modelAsset == null)
            {
                Debug.LogError("❌ [ModelFactory] ModelConfig или ModelAsset null!");
                return null;
            }
            
            var modelName = config.modelAsset.name.ToLower();
            
            // Определяем версию YOLO
            var version = DetectYOLOVersion(modelName);
            
            // Определяем тип задачи
            var taskType = DetectTaskType(modelName);
            
            Debug.Log($"🏭 [ModelFactory] Модель: '{modelName}' → Версия: {version}, Тип: {taskType}");
            
            var model = CreateModelByVersionAndTask(version, taskType);
            
            if (model != null)
                Debug.Log($"✅ [ModelFactory] Создана модель: {model.GetType().Name}");
            
            return model;
        }
        
        /// <summary>
        /// Создает модель явно по версии и типу задачи
        /// </summary>
        public static IInferenceModel CreateModel(YOLOVersion version, YOLOTaskType taskType)
        {
            return CreateModelByVersionAndTask(version, taskType);
        }
        
        #endregion
        
        #region Private Methods
        
        private static YOLOVersion DetectYOLOVersion(string modelName)
        {
            if (modelName.Contains("yolo26") || modelName.Contains("yolov26"))
                return YOLOVersion.YOLO26;
            
            if (modelName.Contains("yolo12") || modelName.Contains("yolov12"))
                return YOLOVersion.YOLO12;
            
            if (modelName.Contains("yolo11") || modelName.Contains("yolov11"))
                return YOLOVersion.YOLO11;
            
            // По умолчанию пробуем YOLO26 (новейшая)
            Debug.LogWarning($"⚠️ [ModelFactory] Не удалось определить версию YOLO из '{modelName}', использую YOLO26");
            return YOLOVersion.YOLO26;
        }
        
        private static YOLOTaskType DetectTaskType(string modelName)
        {
            if (modelName.Contains("pose"))
                return YOLOTaskType.Pose;
            
            if (modelName.Contains("seg"))
                return YOLOTaskType.Segmentation;
            
            if (modelName.Contains("obb"))
                return YOLOTaskType.OBB;
            
            if (modelName.Contains("cls") || modelName.Contains("classify"))
                return YOLOTaskType.Classification;
            
            // По умолчанию - детекция
            return YOLOTaskType.Detection;
        }
        
        private static IInferenceModel CreateModelByVersionAndTask(YOLOVersion version, YOLOTaskType taskType)
        {
            // Приоритет: YOLO26 > YOLO11 > YOLO12
            
            if (version == YOLOVersion.YOLO26)
            {
                return taskType switch
                {
                    YOLOTaskType.Detection => new YOLO26DetectionModel(),
                    YOLOTaskType.Pose => new YOLO26PoseModel(),
                    YOLOTaskType.Segmentation => new YOLO26SegmentationModel(),
                    _ => new YOLO26DetectionModel()
                };
            }
            
            if (version == YOLOVersion.YOLO11)
            {
                return taskType switch
                {
                    YOLOTaskType.Detection => new YOLO11DetectionModel(),
                    YOLOTaskType.Pose => new YOLO11PoseModel(),
                    YOLOTaskType.Segmentation => new SegmentationModel(),
                    _ => new YOLO11DetectionModel()
                };
            }
            
            if (version == YOLOVersion.YOLO12)
            {
                // YOLO12 использует те же парсеры что и YOLO11 (NMS-based)
                return taskType switch
                {
                    YOLOTaskType.Detection => new YOLO11DetectionModel(),
                    YOLOTaskType.Pose => new YOLO11PoseModel(),
                    YOLOTaskType.Segmentation => new SegmentationModel(),
                    _ => new YOLO11DetectionModel()
                };
            }
            
            Debug.LogWarning($"⚠️ [ModelFactory] Неизвестная версия {version}, использую YOLO26");
            return new YOLO26DetectionModel();
        }
        
        #endregion
    }
    
    #region Enums
    
    /// <summary>
    /// Версии YOLO
    /// </summary>
    public enum YOLOVersion
    {
        YOLO11,
        YOLO12,
        YOLO26
    }
    
    /// <summary>
    /// Типы задач YOLO
    /// </summary>
    public enum YOLOTaskType
    {
        Detection,
        Pose,
        Segmentation,
        OBB,
        Classification
    }
    
    #endregion
}
