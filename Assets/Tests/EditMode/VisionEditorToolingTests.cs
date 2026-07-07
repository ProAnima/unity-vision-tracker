using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.InferenceEngine;
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
            AssertMenuPath(
                typeof(VisionControlCenterWindow),
                nameof(VisionControlCenterWindow.Open),
                "Tools/ProAnima Vision/Control Center");
            AssertMenuPath(
                typeof(VisionSetupWizardWindow),
                nameof(VisionSetupWizardWindow.Open),
                "Tools/ProAnima Vision/Advanced/Setup Wizard");
            AssertMenuPath(
                typeof(VisionProfileWizardWindow),
                nameof(VisionProfileWizardWindow.Open),
                "Tools/ProAnima Vision/Advanced/Profile Wizard");
            AssertMenuPath(
                typeof(VisionProfileValidatorWindow),
                nameof(VisionProfileValidatorWindow.Open),
                "Tools/ProAnima Vision/Advanced/Profile Validator");
            AssertMenuPath(
                typeof(VisionProfileCompatibilityWindow),
                nameof(VisionProfileCompatibilityWindow.Open),
                "Tools/ProAnima Vision/Advanced/Profile Compatibility Inspector");
        }

        private static void AssertMenuPath(System.Type type, string methodName, string menuPath)
        {
            MethodInfo open = type.GetMethod(methodName);
            Assert.That(open, Is.Not.Null);

            var menu = open.GetCustomAttribute<MenuItem>();

            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.menuItem, Is.EqualTo(menuPath));
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
        public void PackageManifest_ExposesExperimentalSceneSampleForControlCenterImport()
        {
            Type sampleType = FindType("UnityEditor.PackageManager.UI.Sample");
            Assert.That(sampleType, Is.Not.Null);

            UnityEditor.PackageManager.PackageInfo packageInfo =
                UnityEditor.PackageManager.PackageInfo.FindForAssetPath(
                    "Packages/com.proanima.universal-vision-tracker/package.json");
            Assert.That(packageInfo, Is.Not.Null);

            MethodInfo findByPackage = sampleType.GetMethod(
                "FindByPackage",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string), typeof(string) },
                null);
            Assert.That(findByPackage, Is.Not.Null);

            var samples = findByPackage.Invoke(
                null,
                new object[] { packageInfo.name, packageInfo.version }) as IEnumerable;
            Assert.That(ContainsSample(sampleType, samples, "Experimental Scene"), Is.True);
        }

        [Test]
        public void RepositoryYoloModels_AreImportableAsUnityModelAssets()
        {
            string[] paths =
            {
                "Assets/Models/yolo26n.onnx",
                "Assets/Models/yolo26n-pose.onnx",
                "Assets/Models/yolo26n-seg.onnx"
            };

            foreach (string path in paths)
                Assert.That(AssetDatabase.LoadAssetAtPath<ModelAsset>(path), Is.Not.Null, path);
        }

        [Test]
        public void QuickStartPresets_AreDropdownReady()
        {
            var labels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            Assert.That(VisionQuickStartPresetDefinition.All, Has.Length.EqualTo(7));
            foreach (VisionQuickStartPresetDefinition definition in VisionQuickStartPresetDefinition.All)
            {
                Assert.That(definition.Label, Is.Not.Empty);
                Assert.That(definition.Description, Is.Not.Empty);
                Assert.That(definition.Requirement, Is.Not.Empty);
                Assert.That(definition.ActionLabel, Is.Not.Empty);
                Assert.That(labels.Add(definition.Label), Is.True, definition.Label);
                Assert.That(VisionQuickStartPresetDefinition.FromLabel(definition.Label), Is.SameAs(definition));
            }
        }

        [Test]
        public void QuickStartPresetUtility_RealPipelineDisablesStandalonePreview()
        {
            string utility = System.IO.File.ReadAllText(
                "Packages/com.proanima.universal-vision-tracker/Editor/VisionQuickStartPresetUtility.cs");

            Assert.That(utility, Does.Contain("SetBool(serialized, \"runWebCamPreview\", false)"));
            Assert.That(utility, Does.Contain("SetBool(serialized, \"configureRealPipeline\", true)"));
            Assert.That(utility, Does.Contain("SetEnum(serialized, \"realPipelineSource\""));
            Assert.That(utility, Does.Contain("SetObject(serialized, \"sourceVideoPlayer\""));
            Assert.That(utility, Does.Contain("addDashboard: !useExperimentalSceneBootstrap"));
            Assert.That(utility, Does.Contain("CleanupEditTimeDashboard"));
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

        private static Type FindType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName, false);
                if (type != null)
                    return type;
            }

            return null;
        }

        private static bool ContainsSample(Type sampleType, IEnumerable samples, string displayName)
        {
            if (samples == null)
                return false;

            PropertyInfo displayNameProperty = sampleType.GetProperty("displayName");
            foreach (object sample in samples)
            {
                string sampleName = displayNameProperty?.GetValue(sample) as string;
                if (string.Equals(sampleName, displayName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
