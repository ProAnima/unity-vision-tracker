using NUnit.Framework;
using UnityEngine;
using UniversalTracker.Core;
using UniversalTracker.OutputReceivers;

namespace UniversalTracker.Tests
{
    public sealed class VisionDashboardOverlayStyleTests
    {
        [Test]
        public void DetectionColor_UsesTrackIdAsStableIdentity()
        {
            var first = new VisionDetection
            {
                trackId = 12,
                trackState = VisionTrackState.Tracking,
                classId = 0,
                label = "person",
                confidence = 0.92f
            };
            var second = first;
            second.classId = 7;
            second.label = "tool";

            Color firstColor = VisionDashboardOverlayStyle.DetectionColor(first, 0);
            Color secondColor = VisionDashboardOverlayStyle.DetectionColor(second, 4);

            Assert.That(firstColor.r, Is.EqualTo(secondColor.r).Within(0.0001f));
            Assert.That(firstColor.g, Is.EqualTo(secondColor.g).Within(0.0001f));
            Assert.That(firstColor.b, Is.EqualTo(secondColor.b).Within(0.0001f));
        }

        [Test]
        public void LabelSize_ClampsReadableWidth()
        {
            Vector2 shortLabel = VisionDashboardOverlayStyle.LabelSize("car", 86f, 180f);
            Vector2 longLabel = VisionDashboardOverlayStyle.LabelSize("very long segmentation object label", 86f, 180f);

            Assert.That(shortLabel.x, Is.EqualTo(86f).Within(0.001f));
            Assert.That(longLabel.x, Is.EqualTo(180f).Within(0.001f));
            Assert.That(shortLabel.y, Is.EqualTo(24f).Within(0.001f));
        }

        [Test]
        public void DetectionCornerLength_ClampsToUsefulRange()
        {
            float small = VisionDashboardOverlayStyle.DetectionCornerLength(new Rect(0, 0, 12, 10), 2f);
            float large = VisionDashboardOverlayStyle.DetectionCornerLength(new Rect(0, 0, 800, 600), 2f);

            Assert.That(small, Is.EqualTo(14f).Within(0.001f));
            Assert.That(large, Is.EqualTo(36f).Within(0.001f));
        }

        [Test]
        public void BoneColor_DistinguishesLeftAndRightLimbs()
        {
            Color left = VisionDashboardOverlayStyle.BoneColor(
                new VisionSkeletonBone { from = 5, to = 7, name = "left_upper_arm" },
                0.9f);
            Color right = VisionDashboardOverlayStyle.BoneColor(
                new VisionSkeletonBone { from = 6, to = 8, name = "right_upper_arm" },
                0.9f);

            Assert.That(left.b, Is.GreaterThan(right.b));
            Assert.That(right.r, Is.GreaterThan(left.r));
        }

        [Test]
        public void HaloThickness_ExceedsCoreWithoutExploding()
        {
            float core = VisionDashboardOverlayStyle.ContourCoreThickness(2f);
            float halo = VisionDashboardOverlayStyle.HaloThickness(core);

            Assert.That(halo, Is.GreaterThan(core));
            Assert.That(halo, Is.LessThanOrEqualTo(12f));
        }
    }
}
