using UniversalTracker.Core;

namespace ProAnimaVision.Native
{
    public static class NativeVisionModule
    {
        public const string PackageName = "com.proanima.universal-vision-tracker.native";

        public static VisionOptionalModuleDescriptor Descriptor => new VisionOptionalModuleDescriptor(
            PackageName,
            "Native Runtime",
            "Adds native plugin runtime adapters without making the core package depend on vendor native libraries.",
            new[] { "IVisionRuntimeAdapter", "IVisionModelAdapter", "VisionRawModelOutput" },
            new[] { "platform native plugin" });
    }
}
