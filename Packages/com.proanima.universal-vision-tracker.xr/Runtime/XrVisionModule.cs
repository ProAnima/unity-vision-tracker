using UniversalTracker.Core;

namespace ProAnimaVision.XR
{
    public static class XrVisionModule
    {
        public const string PackageName = "com.proanima.universal-vision-tracker.xr";

        public static VisionOptionalModuleDescriptor Descriptor => new VisionOptionalModuleDescriptor(
            PackageName,
            "XR Passthrough",
            "Adds XR passthrough frame sources without making the core package depend on vendor XR SDKs.",
            new[] { "IVisionFrameSource", "VisionFrame" },
            new[] { "vendor XR passthrough SDK" });
    }
}
