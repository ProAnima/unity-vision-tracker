using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace UniversalTracker.Tests
{
    public sealed class VisionPackageSamplesTests
    {
        [Test]
        public void PackageManifest_DeclaresImportableSamples()
        {
            string manifest = File.ReadAllText(PackagePath("package.json"));

            Assert.That(manifest, Does.Contain("\"samples\""));
            Assert.That(manifest, Does.Contain("\"Minimal Pipeline\""));
            Assert.That(manifest, Does.Contain("\"Dashboard Overlay\""));
            Assert.That(manifest, Does.Contain("\"Samples~/Minimal Pipeline\""));
            Assert.That(manifest, Does.Contain("\"Samples~/Dashboard Overlay\""));
        }

        [Test]
        public void MinimalPipelineSample_ContainsBootstrapAndReadme()
        {
            Assert.That(File.Exists(PackagePath("Samples~/Minimal Pipeline/README.md")), Is.True);
            Assert.That(File.Exists(PackagePath("Samples~/Minimal Pipeline/ProAnimaVisionMinimalPipelineBootstrap.cs")), Is.True);
            Assert.That(File.Exists(PackagePath("Samples~/Minimal Pipeline/ProAnimaVision.Samples.MinimalPipeline.asmdef")), Is.True);
        }

        [Test]
        public void DashboardOverlaySample_ContainsFixtureAndReadme()
        {
            Assert.That(File.Exists(PackagePath("Samples~/Dashboard Overlay/README.md")), Is.True);
            Assert.That(File.Exists(PackagePath("Samples~/Dashboard Overlay/ProAnimaVisionDashboardFixture.cs")), Is.True);
            Assert.That(File.Exists(PackagePath("Samples~/Dashboard Overlay/ProAnimaVision.Samples.DashboardOverlay.asmdef")), Is.True);
        }

        private static string PackagePath(string relativePath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(projectRoot, "Packages", "com.proanima.universal-vision-tracker", relativePath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
