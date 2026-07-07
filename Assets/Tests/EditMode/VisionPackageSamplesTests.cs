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
            Assert.That(manifest, Does.Contain("\"YOLO Model Profiles\""));
            Assert.That(manifest, Does.Contain("\"Experimental Scene\""));
            Assert.That(manifest, Does.Contain("\"Samples~/Minimal Pipeline\""));
            Assert.That(manifest, Does.Contain("\"Samples~/Dashboard Overlay\""));
            Assert.That(manifest, Does.Contain("\"Samples~/YOLO Model Profiles\""));
            Assert.That(manifest, Does.Contain("\"Samples~/Experimental Scene\""));
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

        [Test]
        public void YoloModelProfilesSample_ContainsProfilesLabelsAndReadme()
        {
            Assert.That(File.Exists(PackagePath("Samples~/YOLO Model Profiles/README.md")), Is.True);
            Assert.That(File.Exists(PackagePath("Samples~/YOLO Model Profiles/YoloDetectionCocoProfile.asset")), Is.True);
            Assert.That(File.Exists(PackagePath("Samples~/YOLO Model Profiles/YoloPose2DPersonProfile.asset")), Is.True);
            Assert.That(File.Exists(PackagePath("Samples~/YOLO Model Profiles/YoloSegmentationCocoProfile.asset")), Is.True);
            Assert.That(File.Exists(PackagePath("Samples~/YOLO Model Profiles/coco-80.labels.txt")), Is.True);
            Assert.That(File.Exists(PackagePath("Samples~/YOLO Model Profiles/person.labels.txt")), Is.True);
        }

        [Test]
        public void ExperimentalSceneSample_ContainsSceneBootstrapAndReadme()
        {
            Assert.That(File.Exists(PackagePath("Samples~/Experimental Scene/README.md")), Is.True);
            Assert.That(File.Exists(PackagePath("Samples~/Experimental Scene/ProAnimaVisionExperimentalScene.unity")), Is.True);
            Assert.That(File.Exists(PackagePath("Samples~/Experimental Scene/ProAnimaVisionExperimentalSceneBootstrap.cs")), Is.True);
            Assert.That(File.Exists(PackagePath("Samples~/Experimental Scene/ProAnimaVisionExperimentalSceneCameraControls.cs")), Is.True);
            Assert.That(File.Exists(PackagePath("Samples~/Experimental Scene/ProAnimaVisionExperimentalSceneVideoPlaylist.cs")), Is.True);
            Assert.That(File.Exists(PackagePath("Samples~/Experimental Scene/ProAnimaVision.Samples.ExperimentalScene.asmdef")), Is.True);
            Assert.That(File.Exists(PackagePath("Samples~/Experimental Scene/Editor/ProAnimaVisionExperimentalSceneBootstrapEditor.cs")), Is.True);
            Assert.That(File.Exists(PackagePath("Samples~/Experimental Scene/Editor/ProAnimaVision.ExperimentalScene.Editor.asmdef")), Is.True);
        }

        [Test]
        public void ExperimentalSceneSample_IsWebCamFirst()
        {
            string scene = File.ReadAllText(PackagePath("Samples~/Experimental Scene/ProAnimaVisionExperimentalScene.unity"));

            Assert.That(scene, Does.Contain("runWebCamPreview: 1"));
            Assert.That(scene, Does.Contain("requestedWidth: 1280"));
            Assert.That(scene, Does.Contain("requestedHeight: 720"));
            Assert.That(scene, Does.Contain("previewScaleMode: 2"));
            Assert.That(scene, Does.Not.Contain("runSyntheticPreview"));
            Assert.That(scene, Does.Not.Contain("frameSource:"));
        }

        [Test]
        public void ExperimentalSceneSample_ProvidesRuntimeWebCamControls()
        {
            string bootstrap = File.ReadAllText(PackagePath("Samples~/Experimental Scene/ProAnimaVisionExperimentalSceneCameraControls.cs"));

            Assert.That(bootstrap, Does.Contain("DropdownField"));
            Assert.That(bootstrap, Does.Contain("SelectCamera"));
            Assert.That(bootstrap, Does.Contain("RotatePreview"));
            Assert.That(bootstrap, Does.Contain("ToggleMirror"));
            Assert.That(bootstrap, Does.Contain("RestartPipelineIfNeeded"));
        }

        [Test]
        public void ExperimentalSceneSample_ProvidesRuntimeVideoPlaylistControls()
        {
            string controls = File.ReadAllText(PackagePath("Samples~/Experimental Scene/ProAnimaVisionExperimentalSceneVideoPlaylist.cs"));

            Assert.That(controls, Does.Contain("VideoPlaylistControls"));
            Assert.That(controls, Does.Contain("UsePreviousVideo"));
            Assert.That(controls, Does.Contain("UseNextVideo"));
            Assert.That(controls, Does.Contain("VisionVideoPlaylistSource"));
        }

        private static string PackagePath(string relativePath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(projectRoot, "Packages", "com.proanima.universal-vision-tracker", relativePath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
