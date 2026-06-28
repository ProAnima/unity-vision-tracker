using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UniversalTracker.Core;
using UniversalTracker.Editor;

namespace UniversalTracker.Tests
{
    public sealed class VisionEditorToolingTests
    {
        [Test]
        public void ControlCenterWindow_ExposesTopLevelMenuEntry()
        {
            MethodInfo open = typeof(VisionControlCenterWindow).GetMethod(nameof(VisionControlCenterWindow.Open));
            var menu = open.GetCustomAttribute<MenuItem>();

            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.menuItem, Is.EqualTo("Tools/ProAnima Vision/Control Center"));
        }

        [Test]
        public void ControlCenterWindow_KeepsSampleImportActionAvailable()
        {
            MethodInfo import = typeof(VisionControlCenterWindow).GetMethod(
                "ImportExperimentalScene",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(import, Is.Not.Null);
        }

        [Test]
        public void PipelineProfileTemplate_CanBeCreatedWithoutSelectedModels()
        {
            VisionPipelineProfile profile = VisionModelProfileTemplateFactory.CreatePipelineProfile(null);

            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.models, Is.Empty);
            Assert.That(profile.targetFps, Is.EqualTo(30));

            UnityEngine.Object.DestroyImmediate(profile);
        }
    }
}
