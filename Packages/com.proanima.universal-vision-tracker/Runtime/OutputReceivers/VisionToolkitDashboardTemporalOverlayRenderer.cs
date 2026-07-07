using UnityEngine;
using UnityEngine.UIElements;
using UniversalTracker.Core;

namespace UniversalTracker.OutputReceivers
{
    internal static class VisionToolkitDashboardTemporalOverlayRenderer
    {
        public static int RenderDetections(
            VisionFrameResult result,
            VisionDashboardOverlayState state,
            Vector2 sourceSize,
            Vector2 viewportSize,
            float stroke,
            bool showDetections,
            float overlaySmoothing)
        {
            int used = 0;
            if (!showDetections)
            {
                state.ClearDetectionTemporal();
                return used;
            }

            state.activeDetectionKeys.Clear();
            if (result.detections != null)
            {
                for (int i = 0; i < result.detections.Length; i++)
                {
                    VisionDetection detection = SmoothDetection(result.detections[i], i, state, overlaySmoothing);
                    RenderDetection(detection, state, sourceSize, viewportSize, stroke, used, 1f);
                    used++;
                }
            }

            foreach (var snapshot in state.detectionSnapshots)
            {
                int key = snapshot.Key;
                if (state.activeDetectionKeys.Contains(key) || !state.detectionLastSeen.TryGetValue(key, out int lastSeen))
                    continue;

                int age = state.renderSequence - lastSeen;
                if (age <= 0 || age > VisionDashboardTemporalSmoothing.HoldFrames)
                    continue;

                RenderDetection(
                    snapshot.Value,
                    state,
                    sourceSize,
                    viewportSize,
                    stroke,
                    used,
                    VisionDashboardTemporalSmoothing.HeldOpacity(age));
                used++;
            }

            PruneDetectionTemporal(state);
            return used;
        }

        public static int RenderMasks(
            VisionFrameResult result,
            VisionDashboardOverlayState state,
            Vector2 sourceSize,
            Vector2 viewportSize,
            float stroke,
            bool showMasks,
            float maskAlpha,
            float overlaySmoothing,
            int labelOffset,
            out int contourSegmentsUsed)
        {
            int used = 0;
            contourSegmentsUsed = 0;
            if (!showMasks)
            {
                state.ClearMaskTemporal();
                return used;
            }

            state.activeMaskKeys.Clear();
            if (result.masks != null)
            {
                for (int i = 0; i < result.masks.Length; i++)
                {
                    VisionMask mask = SmoothMask(result.masks[i], i, state, overlaySmoothing);
                    RenderMask(mask, state, sourceSize, viewportSize, stroke, maskAlpha, labelOffset, used, 1f, ref contourSegmentsUsed);
                    used++;
                }
            }

            foreach (var snapshot in state.maskSnapshots)
            {
                int key = snapshot.Key;
                if (state.activeMaskKeys.Contains(key) || !state.maskLastSeen.TryGetValue(key, out int lastSeen))
                    continue;

                int age = state.renderSequence - lastSeen;
                if (age <= 0 || age > VisionDashboardTemporalSmoothing.HoldFrames)
                    continue;

                RenderMask(
                    snapshot.Value,
                    state,
                    sourceSize,
                    viewportSize,
                    stroke,
                    maskAlpha,
                    labelOffset,
                    used,
                    VisionDashboardTemporalSmoothing.HeldOpacity(age),
                    ref contourSegmentsUsed);
                used++;
            }

            PruneMaskTemporal(state);
            return used;
        }

        private static VisionDetection SmoothDetection(VisionDetection detection, int index, VisionDashboardOverlayState state, float smoothing)
        {
            int key = VisionDashboardTemporalSmoothing.DetectionKey(detection, index);
            state.activeDetectionKeys.Add(key);
            detection.normalizedRect = VisionDashboardTemporalSmoothing.SmoothRect(state.smoothedDetectionRects, key, detection.normalizedRect, smoothing);
            state.detectionSnapshots[key] = detection;
            state.detectionLastSeen[key] = state.renderSequence;
            return detection;
        }

        private static VisionMask SmoothMask(VisionMask mask, int index, VisionDashboardOverlayState state, float smoothing)
        {
            int key = VisionDashboardTemporalSmoothing.MaskKey(mask, index);
            state.activeMaskKeys.Add(key);
            mask.normalizedRect = VisionDashboardTemporalSmoothing.SmoothRect(state.smoothedMaskRects, key, mask.normalizedRect, smoothing);
            mask.normalizedContour = VisionDashboardTemporalSmoothing.SmoothPoints(state.smoothedMaskContours, key, mask.normalizedContour, smoothing);
            mask.normalizedContourSegments = VisionDashboardTemporalSmoothing.SmoothPoints(state.smoothedMaskContourSegments, key, mask.normalizedContourSegments, smoothing);
            state.maskSnapshots[key] = mask;
            state.maskLastSeen[key] = state.renderSequence;
            return mask;
        }

        private static void RenderDetection(
            VisionDetection detection,
            VisionDashboardOverlayState state,
            Vector2 sourceSize,
            Vector2 viewportSize,
            float stroke,
            int index,
            float opacity)
        {
            VisualElement box = VisionDashboardElementPool.GetElement(
                state.detections,
                state.detectionLayer,
                VisionToolkitDashboardPrimitives.CreateDetectionBox,
                index);
            Label label = VisionDashboardElementPool.GetLabel(state.labels, state.labelLayer, index);
            VisionToolkitDashboardBoxRenderer.UpdateDetection(box, label, index, detection, sourceSize, viewportSize, stroke, opacity);
        }

        private static void RenderMask(
            VisionMask mask,
            VisionDashboardOverlayState state,
            Vector2 sourceSize,
            Vector2 viewportSize,
            float stroke,
            float maskAlpha,
            int labelOffset,
            int index,
            float opacity,
            ref int contourSegmentsUsed)
        {
            VisualElement element = VisionDashboardElementPool.GetElement(
                state.masks,
                state.maskLayer,
                VisionToolkitDashboardPrimitives.CreateMaskOverlay,
                index);
            Label label = VisionDashboardElementPool.GetLabel(state.labels, state.labelLayer, labelOffset + index);
            Color color = VisionToolkitDashboardBoxRenderer.UpdateMaskOverlay(
                element,
                label,
                index,
                mask,
                sourceSize,
                viewportSize,
                stroke,
                maskAlpha,
                opacity);
            if (mask.HasContour)
                VisionToolkitDashboardMaskContourRenderer.Render(mask, state, sourceSize, viewportSize, stroke, color, opacity, ref contourSegmentsUsed);
        }

        private static void PruneDetectionTemporal(VisionDashboardOverlayState state)
        {
            state.scratchKeys.Clear();
            foreach (var lastSeen in state.detectionLastSeen)
            {
                if (state.renderSequence - lastSeen.Value > VisionDashboardTemporalSmoothing.HoldFrames)
                    state.scratchKeys.Add(lastSeen.Key);
            }

            for (int i = 0; i < state.scratchKeys.Count; i++)
            {
                int key = state.scratchKeys[i];
                state.smoothedDetectionRects.Remove(key);
                state.detectionSnapshots.Remove(key);
                state.detectionLastSeen.Remove(key);
            }
        }

        private static void PruneMaskTemporal(VisionDashboardOverlayState state)
        {
            state.scratchKeys.Clear();
            foreach (var lastSeen in state.maskLastSeen)
            {
                if (state.renderSequence - lastSeen.Value > VisionDashboardTemporalSmoothing.HoldFrames)
                    state.scratchKeys.Add(lastSeen.Key);
            }

            for (int i = 0; i < state.scratchKeys.Count; i++)
            {
                int key = state.scratchKeys[i];
                state.smoothedMaskRects.Remove(key);
                state.smoothedMaskContours.Remove(key);
                state.smoothedMaskContourSegments.Remove(key);
                state.maskSnapshots.Remove(key);
                state.maskLastSeen.Remove(key);
            }
        }
    }
}
