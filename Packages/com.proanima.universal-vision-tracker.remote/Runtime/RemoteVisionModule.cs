using UniversalTracker.Core;

namespace ProAnimaVision.Remote
{
    public static class RemoteVisionModule
    {
        public const string PackageName = "com.proanima.universal-vision-tracker.remote";

        public static VisionOptionalModuleDescriptor Descriptor => new VisionOptionalModuleDescriptor(
            PackageName,
            "Remote Runtime",
            "Adds remote inference runtime adapters without changing the core pipeline.",
            new[] { "IVisionRuntimeAdapter", "IVisionModelAdapter", "VisionFrameResult" },
            new[] { "remote inference service" });
    }
}
