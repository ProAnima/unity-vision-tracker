using NUnit.Framework;
using UnityEngine;
using UnityEngine.Video;
using UniversalTracker.Core;
using UniversalTracker.Editor;
using UniversalTracker.OutputReceivers;

namespace UniversalTracker.Tests
{
    public sealed class VisionSceneSetupUtilityTests
    {
        [TearDown]
        public void TearDown()
        {
            foreach (UniversalTrackerManager manager in Object.FindObjectsByType<UniversalTrackerManager>(FindObjectsSortMode.None))
                Object.DestroyImmediate(manager.gameObject);
        }

        [Test]
        public void CanCreate_RequiresProfile()
        {
            bool canCreate = VisionSceneSetupUtility.CanCreate(null, null, out string reason);

            Assert.That(canCreate, Is.False);
            Assert.That(reason, Does.Contain("Pipeline Profile"));
        }

        [Test]
        public void CreateOrUpdate_WithPipelineProfile_ConfiguresManagerAndDashboard()
        {
            VisionPipelineProfile profile = ScriptableObject.CreateInstance<VisionPipelineProfile>();
            var options = new VisionSceneSetupOptions(
                "Vision Setup Test",
                profile,
                null,
                VisionSceneSetupSource.RenderTexture,
                addDashboard: true,
                enableTracking: true,
                autoStart: false,
                targetFps: 45);

            VisionSceneSetupResult result = VisionSceneSetupUtility.CreateOrUpdate(options);

            Assert.That(result.root.name, Is.EqualTo("Vision Setup Test"));
            Assert.That(result.manager.pipelineProfile, Is.SameAs(profile));
            Assert.That(result.manager.modelProfiles, Is.Null);
            Assert.That(result.manager.inputType, Is.EqualTo(InputProviderType.RenderTexture));
            Assert.That(result.manager.autoStart, Is.False);
            Assert.That(result.manager.targetFPS, Is.EqualTo(45));
            Assert.That(result.manager.useTracking, Is.True);
            Assert.That(result.manager.useToolkitDashboard, Is.True);
            Assert.That(result.dashboard, Is.Not.Null);
            Assert.That(result.manager.manualToolkitDashboardReceiver, Is.SameAs(result.dashboard));

            Object.DestroyImmediate(profile);
        }

        [Test]
        public void CreateOrUpdate_WithModelProfile_ConfiguresSingleModelSetup()
        {
            VisionModelProfile model = ScriptableObject.CreateInstance<VisionModelProfile>();
            var options = new VisionSceneSetupOptions(
                "Vision Model Setup Test",
                null,
                model,
                VisionSceneSetupSource.WebCam,
                addDashboard: false,
                enableTracking: false,
                autoStart: true,
                targetFps: 240);

            VisionSceneSetupResult result = VisionSceneSetupUtility.CreateOrUpdate(options);

            Assert.That(result.manager.pipelineProfile, Is.Null);
            Assert.That(result.manager.modelProfiles, Has.Length.EqualTo(1));
            Assert.That(result.manager.modelProfiles[0], Is.SameAs(model));
            Assert.That(result.manager.inputType, Is.EqualTo(InputProviderType.WebCam));
            Assert.That(result.manager.targetFPS, Is.EqualTo(120));
            Assert.That(result.manager.useTracking, Is.False);
            Assert.That(result.dashboard, Is.Null);
            Assert.That(result.root.GetComponent<VisionToolkitDashboardReceiver>(), Is.Null);

            Object.DestroyImmediate(model);
        }

        [Test]
        public void CreateOrUpdate_WithVideoSource_ConfiguresVideoPlayer()
        {
            VisionPipelineProfile profile = ScriptableObject.CreateInstance<VisionPipelineProfile>();
            var options = new VisionSceneSetupOptions(
                "Vision Video Setup Test",
                profile,
                null,
                VisionSceneSetupSource.Video,
                addDashboard: true,
                enableTracking: true,
                autoStart: false,
                targetFps: 30);

            VisionSceneSetupResult result = VisionSceneSetupUtility.CreateOrUpdate(options);
            VideoPlayer player = result.root.GetComponent<VideoPlayer>();

            Assert.That(result.manager.inputType, Is.EqualTo(InputProviderType.Video));
            Assert.That(player, Is.Not.Null);
            Assert.That(result.manager.sourceVideoPlayer, Is.SameAs(player));
            Assert.That(player.renderMode, Is.EqualTo(VideoRenderMode.APIOnly));
            Assert.That(player.isLooping, Is.True);
            Assert.That(player.playOnAwake, Is.False);
            VisionVideoPlaylistSource playlist = result.root.GetComponent<VisionVideoPlaylistSource>();
            Assert.That(playlist, Is.Not.Null);
            Assert.That(playlist.videoPlayer, Is.SameAs(player));

            Object.DestroyImmediate(profile);
        }

        [Test]
        public void CreateOrUpdate_ReusesExistingTrackerObject()
        {
            VisionModelProfile model = ScriptableObject.CreateInstance<VisionModelProfile>();
            var firstOptions = new VisionSceneSetupOptions(
                "Reusable Vision Setup",
                null,
                model,
                VisionSceneSetupSource.Texture,
                addDashboard: true,
                enableTracking: true,
                autoStart: true,
                targetFps: 30);

            VisionSceneSetupResult first = VisionSceneSetupUtility.CreateOrUpdate(firstOptions);
            VisionSceneSetupResult second = VisionSceneSetupUtility.CreateOrUpdate(firstOptions);

            Assert.That(second.root, Is.SameAs(first.root));
            Assert.That(second.root.GetComponents<UniversalTrackerManager>(), Has.Length.EqualTo(1));
            Assert.That(second.root.GetComponents<VisionToolkitDashboardReceiver>(), Has.Length.EqualTo(1));

            Object.DestroyImmediate(model);
        }
    }
}
