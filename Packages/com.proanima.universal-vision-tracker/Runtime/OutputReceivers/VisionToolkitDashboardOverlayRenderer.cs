using UnityEngine;
using UnityEngine.UIElements;
using UniversalTracker.Core;
using UniversalTracker.Visualization;

namespace UniversalTracker.OutputReceivers
{
    internal static class VisionToolkitDashboardOverlayRenderer
    {
        public static void Clear(VisionDashboardOverlayState state)
        {
            VisionDashboardElementPool.SetPoolActive(state.masks, 0);
            VisionDashboardElementPool.SetPoolActive(state.detections, 0);
            VisionDashboardElementPool.SetLabelsActive(state.labels, 0);
            VisionDashboardElementPool.SetPoolActive(state.keypoints, 0);
            VisionDashboardElementPool.SetPoolActive(state.bones, 0);
            VisionDashboardElementPool.SetPoolActive(state.maskContourSegments, 0);
            state.ClearTemporal();
        }

        public static void Render(
            VisionFrameResult result,
            VisionDashboardOverlayState state,
            Vector2 sourceSize,
            Vector2 viewportSize,
            float stroke,
            bool showMasks,
            bool showDetections,
            bool showPoses,
            float maskAlpha,
            float keypointConfidenceThreshold,
            float poseSmoothing)
        {
            state.renderSequence++;
            float overlaySmoothing = Mathf.Clamp01(poseSmoothing);
            int detectionsUsed = VisionToolkitDashboardTemporalOverlayRenderer.RenderDetections(
                result,
                state,
                sourceSize,
                viewportSize,
                stroke,
                showDetections,
                showMasks,
                overlaySmoothing);
            int masksUsed = VisionToolkitDashboardTemporalOverlayRenderer.RenderMasks(
                result,
                state,
                sourceSize,
                viewportSize,
                stroke,
                showMasks,
                maskAlpha,
                overlaySmoothing,
                detectionsUsed,
                out int maskContourSegmentsUsed);
            RenderPoses(
                result,
                state,
                sourceSize,
                viewportSize,
                stroke,
                showPoses,
                keypointConfidenceThreshold,
                overlaySmoothing,
                out int keypointsUsed,
                out int bonesUsed);

            VisionDashboardElementPool.SetPoolActive(state.masks, masksUsed);
            VisionDashboardElementPool.SetPoolActive(state.detections, detectionsUsed);
            VisionDashboardElementPool.SetLabelsActive(state.labels, detectionsUsed + masksUsed);
            VisionDashboardElementPool.SetPoolActive(state.keypoints, keypointsUsed);
            VisionDashboardElementPool.SetPoolActive(state.bones, bonesUsed);
            VisionDashboardElementPool.SetPoolActive(state.maskContourSegments, maskContourSegmentsUsed);
        }

        private static void RenderPoses(
            VisionFrameResult result,
            VisionDashboardOverlayState state,
            Vector2 sourceSize,
            Vector2 viewportSize,
            float stroke,
            bool showPoses,
            float keypointConfidenceThreshold,
            float poseSmoothing,
            out int keypointsUsed,
            out int bonesUsed)
        {
            keypointsUsed = 0;
            bonesUsed = 0;
            if (!showPoses)
            {
                state.ClearPoseTemporal();
                return;
            }

            if (result.poses == null)
            {
                PruneKeypointTemporal(state);
                return;
            }

            state.activeKeypointKeys.Clear();

            for (int i = 0; i < result.poses.Length; i++)
                UpdatePose(result.poses[i], i, state, sourceSize, viewportSize, stroke, keypointConfidenceThreshold, poseSmoothing, ref keypointsUsed, ref bonesUsed);

            PruneKeypointTemporal(state);
        }

        private static void UpdatePose(
            VisionPose pose,
            int poseIndex,
            VisionDashboardOverlayState state,
            Vector2 sourceSize,
            Vector2 viewportSize,
            float stroke,
            float keypointConfidenceThreshold,
            float poseSmoothing,
            ref int keypointsUsed,
            ref int bonesUsed)
        {
            if (pose.keypoints == null)
                return;

            if (pose.skeleton.bones != null)
            {
                for (int i = 0; i < pose.skeleton.bones.Length; i++)
                {
                    VisionSkeletonBone bone = pose.skeleton.bones[i];
                    if (!TryGetVisibleKeypoint(pose, bone.from, keypointConfidenceThreshold, out VisionKeypoint from) ||
                        !TryGetVisibleKeypoint(pose, bone.to, keypointConfidenceThreshold, out VisionKeypoint to))
                    {
                        continue;
                    }

                    Vector2 fromPoint = ResolvePosePoint(pose, poseIndex, from, state, sourceSize, viewportSize, poseSmoothing);
                    Vector2 toPoint = ResolvePosePoint(pose, poseIndex, to, state, sourceSize, viewportSize, poseSmoothing);
                    VisualElement boneElement = VisionDashboardElementPool.GetElement(
                        state.bones,
                        state.boneLayer,
                        VisionToolkitDashboardPrimitives.CreateBone,
                        bonesUsed);
                    UpdateBone(boneElement, bone, fromPoint, toPoint, from.confidence, to.confidence, stroke);
                    bonesUsed++;
                }
            }

            for (int i = 0; i < pose.keypoints.Length; i++)
            {
                if (!IsVisibleKeypoint(pose.keypoints[i], keypointConfidenceThreshold))
                    continue;

                Vector2 point = ResolvePosePoint(pose, poseIndex, pose.keypoints[i], state, sourceSize, viewportSize, poseSmoothing);
                VisualElement keypoint = VisionDashboardElementPool.GetElement(
                    state.keypoints,
                    state.keypointLayer,
                    VisionToolkitDashboardPrimitives.CreateKeypoint,
                    keypointsUsed);
                UpdateKeypoint(keypoint, point, pose.keypoints[i], stroke);
                keypointsUsed++;
            }
        }

        private static void UpdateBone(VisualElement bone, VisionSkeletonBone skeletonBone, Vector2 from, Vector2 to, float fromConfidence, float toConfidence, float stroke)
        {
            float confidence = Mathf.Clamp01(Mathf.Min(fromConfidence, toConfidence));
            float core = VisionDashboardOverlayStyle.BoneCoreThickness(stroke, confidence);
            float halo = VisionDashboardOverlayStyle.HaloThickness(core);
            Color color = VisionDashboardOverlayStyle.BoneColor(skeletonBone, confidence);
            VisionToolkitDashboardLineRenderer.UpdateLine(bone, from, to, core, halo, color, 0.5f);
        }

        private static void UpdateKeypoint(VisualElement keypoint, Vector2 point, VisionKeypoint data, float stroke)
        {
            float radius = VisionDashboardOverlayStyle.KeypointRadius(stroke, data.confidence);
            keypoint.style.left = point.x - radius;
            keypoint.style.top = point.y - radius;
            keypoint.style.width = radius * 2f;
            keypoint.style.height = radius * 2f;
            keypoint.style.backgroundColor = VisionDashboardOverlayStyle.WithAlpha(Color.black, 0.62f);
            VisionDashboardTheme.SetBorderColor(keypoint, new Color(1f, 1f, 1f, 0.35f));

            VisualElement core = keypoint.childCount > 0 ? keypoint.ElementAt(0) : null;
            if (core == null)
                return;

            float inset = Mathf.Clamp(radius * 0.34f, 1.5f, 3.5f);
            core.style.left = inset;
            core.style.right = inset;
            core.style.top = inset;
            core.style.bottom = inset;
            core.style.backgroundColor = VisionDashboardOverlayStyle.KeypointColor(data);
        }

        private static Vector2 ResolvePosePoint(
            VisionPose pose,
            int poseIndex,
            VisionKeypoint keypoint,
            VisionDashboardOverlayState state,
            Vector2 sourceSize,
            Vector2 viewportSize,
            float poseSmoothing)
        {
            Vector2 current = VisionDashboardGeometry.NormalizedToViewportPoint(keypoint.normalizedPosition, sourceSize, viewportSize);
            float smoothing = Mathf.Clamp01(poseSmoothing);
            if (smoothing <= 0f)
                return current;

            int key = VisionDashboardTemporalSmoothing.PoseKey(pose, poseIndex, keypoint.index);
            state.activeKeypointKeys.Add(key);
            state.keypointLastSeen[key] = state.renderSequence;
            return VisionDashboardTemporalSmoothing.SmoothPoint(state.smoothedKeypoints, key, current, smoothing);
        }

        private static void PruneKeypointTemporal(VisionDashboardOverlayState state)
        {
            state.scratchKeys.Clear();
            foreach (var lastSeen in state.keypointLastSeen)
            {
                if (state.renderSequence - lastSeen.Value > VisionDashboardTemporalSmoothing.HoldFrames)
                    state.scratchKeys.Add(lastSeen.Key);
            }

            for (int i = 0; i < state.scratchKeys.Count; i++)
            {
                int key = state.scratchKeys[i];
                state.smoothedKeypoints.Remove(key);
                state.keypointLastSeen.Remove(key);
            }
        }

        private static bool TryGetVisibleKeypoint(VisionPose pose, int index, float threshold, out VisionKeypoint keypoint)
        {
            keypoint = default;
            if (pose.keypoints == null || index < 0 || index >= pose.keypoints.Length)
                return false;

            keypoint = pose.keypoints[index];
            return IsVisibleKeypoint(keypoint, threshold);
        }

        private static bool IsVisibleKeypoint(VisionKeypoint keypoint, float threshold)
        {
            return keypoint.isVisible && keypoint.confidence >= threshold;
        }
    }
}
