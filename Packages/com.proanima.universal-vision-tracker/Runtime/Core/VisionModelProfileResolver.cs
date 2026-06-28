namespace UniversalTracker.Core
{
    internal static class VisionModelProfileResolver
    {
        public static VisionModelProfile[] GetProfiles(VisionPipelineProfile pipelineProfile, VisionModelProfile[] fallbackProfiles)
        {
            if (pipelineProfile != null && pipelineProfile.HasModels)
                return pipelineProfile.models;

            return fallbackProfiles;
        }

        public static VisionModelProfile Resolve(
            VisionPipelineProfile pipelineProfile,
            VisionModelProfile[] fallbackProfiles,
            ref int activeModelIndex)
        {
            VisionModelProfile[] profiles = GetProfiles(pipelineProfile, fallbackProfiles);
            if (profiles == null || profiles.Length == 0)
                return null;

            if (activeModelIndex < 0 || activeModelIndex >= profiles.Length)
                activeModelIndex = 0;

            return profiles[activeModelIndex];
        }
    }
}
