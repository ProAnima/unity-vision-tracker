using UnityEngine;

namespace UniversalTracker.Core
{
    internal static class VisionProfileValidationLogger
    {
        public static void Log(string owner, VisionModelProfile profile, VisionProfileValidationReport report)
        {
            if (report == null || report.Messages.Count == 0)
                return;

            string profileName = profile != null ? profile.name : "null";
            foreach (VisionValidationMessage message in report.Messages)
            {
                string log = $"[{owner}] Profile validation '{profileName}': {message.code} - {message.message}";
                if (message.severity == VisionValidationSeverity.Error)
                    Debug.LogError(log);
                else if (message.severity == VisionValidationSeverity.Warning)
                    Debug.LogWarning(log);
                else
                    Debug.Log(log);
            }
        }
    }
}
