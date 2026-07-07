using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UniversalTracker;
using UniversalTracker.Core;
using UniversalTracker.OutputReceivers;

namespace UniversalTracker.Tests
{
    public sealed class VisionToolkitDashboardPlayModeTests
    {
        [UnityTest]
        public IEnumerator DashboardReceiver_BuildsRuntimeVisualTreeAndConsumesFrameResult()
        {
            var go = new GameObject("VisionDashboardPlayModeTest");
            var document = go.AddComponent<UIDocument>();
            LogAssert.Expect(LogType.Warning, "No Theme Style Sheet set to PanelSettings , UI will not render properly");
            document.panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            var receiver = go.AddComponent<VisionToolkitDashboardReceiver>();
            receiver.autoFindManager = false;
            receiver.subscribeToManagerEvent = false;
            var texture = new Texture2D(64, 48);

            yield return null;

            VisionFrameResult result = CreateFrameResult();
            receiver.ReceiveVisionResult(result, texture);

            yield return null;

            Assert.That(document.rootVisualElement, Is.Not.Null);
            Assert.That(document.rootVisualElement.Q("VisionPreviewStage"), Is.Not.Null);
            Assert.That(document.rootVisualElement.Q("VisionOverlay"), Is.Not.Null);
            Assert.That(document.rootVisualElement.childCount, Is.GreaterThan(0));

            Object.Destroy(texture);
            Object.Destroy(document.panelSettings);
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator DashboardReceiver_BindsRuntimeControlsToManagerAndProfile()
        {
            var go = new GameObject("VisionDashboardControlsPlayModeTest");
            var document = go.AddComponent<UIDocument>();
            LogAssert.Expect(LogType.Warning, "No Theme Style Sheet set to PanelSettings , UI will not render properly");
            document.panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            var manager = go.AddComponent<UniversalTrackerManager>();
            manager.autoStart = false;
            manager.pipelineProfile = ScriptableObject.CreateInstance<VisionPipelineProfile>();
            var model = ScriptableObject.CreateInstance<VisionModelProfile>();
            model.confidenceThreshold = 0.35f;
            model.nmsThreshold = 0.45f;
            manager.pipelineProfile.models = new[] { model };
            var receiver = go.AddComponent<VisionToolkitDashboardReceiver>();
            receiver.trackerManager = manager;
            receiver.autoFindManager = false;
            receiver.subscribeToManagerEvent = false;

            yield return null;

            Toggle overlay = document.rootVisualElement.Q<Toggle>("VisionToggleOverlay");
            Slider confidence = document.rootVisualElement.Q<Slider>("VisionConfidenceSlider");
            Slider nms = document.rootVisualElement.Q<Slider>("VisionNmsSlider");
            Slider fps = document.rootVisualElement.Q<Slider>("VisionTargetFpsSlider");

            Assert.That(overlay, Is.Not.Null);
            Assert.That(confidence, Is.Not.Null);
            Assert.That(nms, Is.Not.Null);
            Assert.That(fps, Is.Not.Null);

            overlay.value = false;
            confidence.value = 0.72f;
            nms.value = 0.31f;
            fps.value = 24f;

            yield return null;

            Assert.That(receiver.showVisualization, Is.False);
            Assert.That(model.confidenceThreshold, Is.EqualTo(0.72f).Within(0.0001f));
            Assert.That(model.nmsThreshold, Is.EqualTo(0.31f).Within(0.0001f));
            Assert.That(manager.targetFPS, Is.EqualTo(24));

            Object.DestroyImmediate(model);
            Object.DestroyImmediate(manager.pipelineProfile);
            Object.Destroy(document.panelSettings);
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator OverlayRenderer_CreatesDetectionPoseAndMaskElements()
        {
            var maskTexture = new Texture2D(4, 4);
            var state = new VisionDashboardOverlayState
            {
                maskLayer = new VisualElement(),
                detectionLayer = new VisualElement(),
                boneLayer = new VisualElement(),
                keypointLayer = new VisualElement(),
                labelLayer = new VisualElement()
            };

            VisionToolkitDashboardOverlayRenderer.Render(
                CreateFrameResult(maskTexture),
                state,
                new Vector2(640, 480),
                new Vector2(640, 480),
                2f,
                true,
                true,
                true,
                0.25f,
                0.1f,
                0.55f);

            yield return null;

            Assert.That(state.detections.Count, Is.EqualTo(1));
            Assert.That(state.labels.Count, Is.EqualTo(2));
            Assert.That(state.masks.Count, Is.EqualTo(1));
            Assert.That(state.maskContourSegments.Count, Is.EqualTo(4));
            Assert.That(state.keypoints.Count, Is.GreaterThan(0));
            Assert.That(state.bones.Count, Is.GreaterThan(0));
            Assert.That(state.detectionLayer.childCount, Is.EqualTo(1));
            Assert.That(state.labelLayer.childCount, Is.EqualTo(2));
            Assert.That(state.labels[1].text, Does.Contain("person"));

            Object.Destroy(maskTexture);
        }

        [UnityTest]
        public IEnumerator OverlayRenderer_HoldsMissingDetectionAndMaskBriefly()
        {
            var state = new VisionDashboardOverlayState
            {
                maskLayer = new VisualElement(),
                detectionLayer = new VisualElement(),
                boneLayer = new VisualElement(),
                keypointLayer = new VisualElement(),
                labelLayer = new VisualElement()
            };

            VisionToolkitDashboardOverlayRenderer.Render(
                CreateFrameResult(),
                state,
                new Vector2(640, 480),
                new Vector2(640, 480),
                2f,
                true,
                true,
                false,
                0.25f,
                0.1f,
                0.55f);
            VisionToolkitDashboardOverlayRenderer.Render(
                EmptyFrameResult(),
                state,
                new Vector2(640, 480),
                new Vector2(640, 480),
                2f,
                true,
                true,
                false,
                0.25f,
                0.1f,
                0.55f);

            yield return null;

            Assert.That(state.detections[0].style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(state.masks[0].style.display.value, Is.EqualTo(DisplayStyle.Flex));

            for (int i = 0; i < 3; i++)
            {
                VisionToolkitDashboardOverlayRenderer.Render(
                    EmptyFrameResult(),
                    state,
                    new Vector2(640, 480),
                    new Vector2(640, 480),
                    2f,
                    true,
                    true,
                    false,
                    0.25f,
                    0.1f,
                    0.55f);
            }

            yield return null;

            Assert.That(state.detections[0].style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(state.masks[0].style.display.value, Is.EqualTo(DisplayStyle.None));
        }

        private static VisionFrameResult CreateFrameResult(Texture2D maskTexture = null)
        {
            return new VisionFrameResult
            {
                frameIndex = 1,
                timestamp = 1d,
                sourceSize = new Vector2Int(640, 480),
                detections = new[]
                {
                    new VisionDetection
                    {
                        trackId = 7,
                        classId = 0,
                        label = "person",
                        confidence = 0.92f,
                        normalizedRect = new Rect(0.25f, 0.25f, 0.5f, 0.5f),
                        trackState = VisionTrackState.Tracking
                    }
                },
                masks = new[]
                {
                    new VisionMask
                    {
                        trackId = 7,
                        classId = 0,
                        label = "person",
                        confidence = 0.88f,
                        texture = maskTexture,
                        normalizedRect = new Rect(0.25f, 0.25f, 0.5f, 0.5f),
                        normalizedContour = new[]
                        {
                            new Vector2(0.25f, 0.25f),
                            new Vector2(0.75f, 0.25f),
                            new Vector2(0.75f, 0.75f),
                            new Vector2(0.25f, 0.75f)
                        }
                    }
                },
                poses = new[]
                {
                    new VisionPose
                    {
                        personId = 7,
                        confidence = 0.9f,
                        normalizedRect = new Rect(0.25f, 0.25f, 0.5f, 0.5f),
                        keypoints = CreatePoseKeypoints(),
                        skeleton = new VisionSkeletonDefinition
                        {
                            name = "smoke",
                            bones = new[]
                            {
                                new VisionSkeletonBone { from = 5, to = 6, name = "shoulders" },
                                new VisionSkeletonBone { from = 11, to = 12, name = "hips" }
                            }
                        }
                    }
                }
            };
        }

        private static VisionFrameResult EmptyFrameResult()
        {
            return new VisionFrameResult
            {
                frameIndex = 2,
                timestamp = 2d,
                sourceSize = new Vector2Int(640, 480),
                detections = new VisionDetection[0],
                masks = new VisionMask[0],
                poses = new VisionPose[0]
            };
        }

        private static VisionKeypoint Keypoint(int index, float x, float y)
        {
            return new VisionKeypoint
            {
                index = index,
                name = index.ToString(),
                normalizedPosition = new Vector2(x, y),
                confidence = 0.9f,
                isVisible = true
            };
        }

        private static VisionKeypoint[] CreatePoseKeypoints()
        {
            var keypoints = new VisionKeypoint[17];
            keypoints[5] = Keypoint(5, 0.35f, 0.35f);
            keypoints[6] = Keypoint(6, 0.65f, 0.35f);
            keypoints[11] = Keypoint(11, 0.42f, 0.68f);
            keypoints[12] = Keypoint(12, 0.58f, 0.68f);
            return keypoints;
        }
    }
}
