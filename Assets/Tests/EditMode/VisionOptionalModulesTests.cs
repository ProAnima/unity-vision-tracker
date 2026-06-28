using System.IO;
using NUnit.Framework;
using UnityEngine;
using UniversalTracker.Core;

namespace UniversalTracker.Tests
{
    public sealed class VisionOptionalModulesTests
    {
        private static readonly string[] ModulePackageNames =
        {
            "com.proanima.universal-vision-tracker.ar-foundation",
            "com.proanima.universal-vision-tracker.xr",
            "com.proanima.universal-vision-tracker.mediapipe",
            "com.proanima.universal-vision-tracker.native",
            "com.proanima.universal-vision-tracker.remote"
        };

        [Test]
        public void OptionalModuleDescriptor_RequiresPackageDisplayNameAndExtensionPoints()
        {
            var descriptor = new VisionOptionalModuleDescriptor(
                "com.proanima.test",
                "Test Module",
                "Test purpose",
                new[] { "IVisionRuntimeAdapter" },
                new[] { "vendor.sdk" });

            Assert.That(descriptor.IsValid, Is.True);
            Assert.That(descriptor.extensionPoints, Does.Contain("IVisionRuntimeAdapter"));
            Assert.That(descriptor.optionalDependencies, Does.Contain("vendor.sdk"));
        }

        [Test]
        public void OptionalModulePackages_HaveManifestsAndReadmes()
        {
            foreach (string packageName in ModulePackageNames)
            {
                Assert.That(File.Exists(PackagePath(packageName, "package.json")), Is.True, packageName);
                Assert.That(File.Exists(PackagePath(packageName, "README.md")), Is.True, packageName);
                Assert.That(File.Exists(PackagePath(packageName, "CHANGELOG.md")), Is.True, packageName);
            }
        }

        [Test]
        public void OptionalModulePackages_DependOnCorePackageOnly()
        {
            foreach (string packageName in ModulePackageNames)
            {
                string manifest = File.ReadAllText(PackagePath(packageName, "package.json"));

                Assert.That(manifest, Does.Contain("\"com.proanima.universal-vision-tracker\""));
                Assert.That(manifest, Does.Not.Contain("\"com.unity.xr.arfoundation\""));
                Assert.That(manifest, Does.Not.Contain("\"com.unity.xr.management\""));
            }
        }

        [Test]
        public void CorePackage_DoesNotDependOnOptionalModules()
        {
            string manifest = File.ReadAllText(PackagePath("com.proanima.universal-vision-tracker", "package.json"));

            foreach (string packageName in ModulePackageNames)
                Assert.That(manifest, Does.Not.Contain(packageName));
        }

        private static string PackagePath(string packageName, string relativePath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(projectRoot, "Packages", packageName, relativePath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
