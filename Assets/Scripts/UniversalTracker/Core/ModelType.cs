using UnityEngine;

namespace UniversalTracker.Core
{
    /// <summary>
    /// Типы поддерживаемых моделей нейросетей
    /// </summary>
    public enum ModelType
    {
        Detection,
        Pose,
        Segmentation,
        OBB,
        Classification
    }

    /// <summary>
    /// Уровни уверенности детекции
    /// </summary>
    public enum DetectionLevel
    {
        None = 0,
        Low = 1,
        Medium = 2,
        High = 3
    }
}
