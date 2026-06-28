using UniversalTracker.Core;

namespace ProAnimaVision.ARFoundation
{
    public static class ArFoundationVisionModule
    {
        public const string PackageName = "com.proanima.universal-vision-tracker.ar-foundation";

        public static VisionOptionalModuleDescriptor Descriptor => new VisionOptionalModuleDescriptor(
            PackageName,
            "AR Foundation",
            "Adds AR Foundation camera frame sources without making the core package depend on AR Foundation.",
            new[] { "IVisionFrameSource", "VisionFrame" },
            new[] { "com.unity.xr.arfoundation" });
    }
}
