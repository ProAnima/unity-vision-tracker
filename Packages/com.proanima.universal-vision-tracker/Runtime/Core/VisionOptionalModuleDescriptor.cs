using System;

namespace UniversalTracker.Core
{
    [Serializable]
    public readonly struct VisionOptionalModuleDescriptor
    {
        public readonly string packageName;
        public readonly string displayName;
        public readonly string purpose;
        public readonly string[] extensionPoints;
        public readonly string[] optionalDependencies;

        public VisionOptionalModuleDescriptor(
            string packageName,
            string displayName,
            string purpose,
            string[] extensionPoints,
            string[] optionalDependencies)
        {
            this.packageName = packageName ?? string.Empty;
            this.displayName = displayName ?? string.Empty;
            this.purpose = purpose ?? string.Empty;
            this.extensionPoints = extensionPoints ?? Array.Empty<string>();
            this.optionalDependencies = optionalDependencies ?? Array.Empty<string>();
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(packageName) &&
                               !string.IsNullOrWhiteSpace(displayName) &&
                               extensionPoints.Length > 0;
    }
}
