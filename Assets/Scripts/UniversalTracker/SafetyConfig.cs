using UnityEngine;

namespace UniversalTracker
{
    /// <summary>
    /// Настройки безопасности для предотвращения крашей
    /// </summary>
    [CreateAssetMenu(fileName = "SafetyConfig", menuName = "Universal Tracker/Safety Config")]
    public class SafetyConfig : ScriptableObject
    {
        [Header("🛡️ Защита от крашей")]
        [Tooltip("Максимальное время ожидания одного inference (мс)")]
        public float maxInferenceTime = 1000f;
        
        [Tooltip("Пропускать кадры если inference слишком долгий")]
        public bool skipFramesOnSlowInference = true;
        
        [Tooltip("Автоматически перезапускать при критических ошибках")]
        public bool autoRestartOnCriticalError = false;
        
        [Tooltip("Максимум ошибок подряд до остановки")]
        public int maxConsecutiveErrors = 10;
        
        [Header("🔍 Отладка")]
        [Tooltip("Дополнительные логи для отладки")]
        public bool verboseLogging = false;
        
        [Tooltip("Логировать каждый кадр")]
        public bool logEveryFrame = false;
        
        [Tooltip("Останавливать на первой ошибке")]
        public bool stopOnFirstError = false;
    }
}
