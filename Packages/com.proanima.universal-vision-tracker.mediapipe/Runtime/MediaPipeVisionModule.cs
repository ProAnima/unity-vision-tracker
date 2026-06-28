using UniversalTracker.Core;

namespace ProAnimaVision.MediaPipe
{
    public static class MediaPipeVisionModule
    {
        public const string PackageName = "com.proanima.universal-vision-tracker.mediapipe";

        public static VisionOptionalModuleDescriptor Descriptor => new VisionOptionalModuleDescriptor(
            PackageName,
            "MediaPipe",
            "Adds MediaPipe runtime adapters and parsers without making the core package depend on MediaPipe bindings.",
            new[] { "IVisionRuntimeAdapter", "IVisionModelAdapter", "IVisionOutputParser" },
            new[] { "MediaPipe Unity binding" });
    }
}
