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
            Assert.That(manifest, Does.Contain("\"YOLO Humanoid Retargeting\""));
            Assert.That(manifest, Does.Contain("\"Samples~/Minimal Pipeline\""));
            Assert.That(manifest, Does.Contain("\"Samples~/Dashboard Overlay\""));
            Assert.That(manifest, Does.Contain("\"Samples~/YOLO Model Profiles\""));
            Assert.That(manifest, Does.Contain("\"Samples~/Experimental Scene\""));
            Assert.That(manifest, Does.Contain("\"Samples~/YOLO Humanoid Retargeting\""));
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
        public void YoloHumanoidRetargetingSample_ContainsDemoRigAndReadme()
        {
            Assert.That(File.Exists(PackagePath("Samples~/YOLO Humanoid Retargeting/README.md")), Is.True);
            Assert.That(File.Exists(PackagePath("Samples~/YOLO Humanoid Retargeting/ProAnimaVision.Samples.YoloHumanoidRetargeting.asmdef")), Is.True);
            Assert.That(File.Exists(PackagePath("Samples~/YOLO Humanoid Retargeting/ProAnimaVisionGeneratedHumanoidRig.cs")), Is.True);
            Assert.That(File.Exists(PackagePath("Samples~/YOLO Humanoid Retargeting/ProAnimaVisionRetargetingSourceController.cs")), Is.True);
            Assert.That(File.Exists(PackagePath("Samples~/YOLO Humanoid Retargeting/ProAnimaVisionRetargetingDemoView.cs")), Is.True);
            Assert.That(File.Exists(PackagePath("Samples~/YOLO Humanoid Retargeting/ProAnimaVisionRetargetingPreviewOverlay.cs")), Is.True);
            Assert.That(File.Exists(PackagePath("Samples~/YOLO Humanoid Retargeting/ProAnimaVisionYoloHumanoidRetargetingDemo.cs")), Is.True);
        }

        [Test]
        public void YoloHumanoidRetargetingSample_ProvidesSplitSourcePreview()
        {
            string demo = File.ReadAllText(PackagePath("Samples~/YOLO Humanoid Retargeting/ProAnimaVisionYoloHumanoidRetargetingDemo.cs"));
            string view = File.ReadAllText(PackagePath("Samples~/YOLO Humanoid Retargeting/ProAnimaVisionRetargetingDemoView.cs"));
            string overlay = File.ReadAllText(PackagePath("Samples~/YOLO Humanoid Retargeting/ProAnimaVisionRetargetingPreviewOverlay.cs"));
            string source = File.ReadAllText(PackagePath("Samples~/YOLO Humanoid Retargeting/ProAnimaVisionRetargetingSourceController.cs"));
            string asmdef = File.ReadAllText(PackagePath("Samples~/YOLO Humanoid Retargeting/ProAnimaVision.Samples.YoloHumanoidRetargeting.asmdef"));

            Assert.That(demo, Does.Contain("CreateDetection"));
            Assert.That(demo, Does.Contain("sourceTexture"));
            Assert.That(demo, Does.Contain("camera.rect = new Rect(0.5f"));
            Assert.That(demo, Does.Contain("UniversalTrackerManager"));
            Assert.That(demo, Does.Contain("poseModelProfile"));
            Assert.That(demo, Does.Contain("ShouldUseLivePipeline"));
            Assert.That(demo, Does.Contain("Synthetic retargeting fixture"));
            Assert.That(view, Does.Contain("UIDocument"));
            Assert.That(view, Does.Contain("DropdownField"));
            Assert.That(view, Does.Contain("RefreshCameraChoices"));
            Assert.That(overlay, Does.Not.Contain("1f - normalized.y"));
            Assert.That(source, Does.Contain("WebCamFrameSource"));
            Assert.That(source, Does.Contain("VideoFrameSource"));
            Assert.That(source, Does.Contain("VisionVideoPlaylistSource"));
            Assert.That(source, Does.Contain("ownsFrameSource"));
            Assert.That(source, Does.Contain("Default Camera"));
            Assert.That(asmdef, Does.Not.Contain("\"UnityEngine.UI\""));
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
        public void ExperimentalSceneSample_GatesStandaloneWebCamPreviewToWebCamSource()
        {
            string bootstrap = File.ReadAllText(PackagePath("Samples~/Experimental Scene/ProAnimaVisionExperimentalSceneBootstrap.cs"));
            string controls = File.ReadAllText(PackagePath("Samples~/Experimental Scene/ProAnimaVisionExperimentalSceneCameraControls.cs"));

            Assert.That(bootstrap, Does.Contain("ShouldRunStandaloneWebCamPreview()"));
            Assert.That(bootstrap, Does.Contain("realPipelineSource == InputProviderType.WebCam"));
            Assert.That(bootstrap, Does.Contain("AdoptExistingVideoSourceIfNeeded"));
            Assert.That(bootstrap, Does.Contain("manager.inputType != InputProviderType.Video"));
            Assert.That(controls, Does.Contain("UpdateCameraControls"));
            Assert.That(controls, Does.Contain("realPipelineSource == InputProviderType.WebCam"));
        }

        [Test]
        public void ExperimentalSceneSample_PreservesConfiguredManagerProfilesOnPlay()
        {
            string bootstrap = File.ReadAllText(PackagePath("Samples~/Experimental Scene/ProAnimaVisionExperimentalSceneBootstrap.cs"));

            Assert.That(bootstrap, Does.Contain("AdoptExistingProfilesIfNeeded"));
            Assert.That(bootstrap, Does.Contain("ApplyManagerProfiles"));
            Assert.That(bootstrap, Does.Contain("pipelineProfile = manager.pipelineProfile"));
            Assert.That(bootstrap, Does.Contain("modelProfile = manager.modelProfiles[0]"));
            Assert.That(bootstrap, Does.Not.Contain("manager.modelProfiles = pipelineProfile == null && modelProfile != null"));
        }

        [Test]
        public void ExperimentalSceneSample_ProvidesRuntimeVideoPlaylistControls()
        {
            string controls = File.ReadAllText(PackagePath("Samples~/Experimental Scene/ProAnimaVisionExperimentalSceneVideoPlaylist.cs"));

            Assert.That(controls, Does.Contain("VideoPlaylistControls"));
            Assert.That(controls, Does.Contain("UsePreviousVideo"));
            Assert.That(controls, Does.Contain("UseNextVideo"));
            Assert.That(controls, Does.Contain("RegisterVideoHotkeys"));
            Assert.That(controls, Does.Contain("KeyDownEvent"));
            Assert.That(controls, Does.Contain("KeyCode.LeftArrow"));
            Assert.That(controls, Does.Contain("KeyCode.RightArrow"));
            Assert.That(controls, Does.Not.Contain("Input.GetKeyDown"));
            Assert.That(controls, Does.Contain("VisionVideoPlaylistSource"));
        }

        private static string PackagePath(string relativePath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(projectRoot, "Packages", "com.proanima.universal-vision-tracker", relativePath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
