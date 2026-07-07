using UnityEngine;

namespace UniversalTracker.Core
{
    public sealed partial class VisionCocoHumanoidPoseRetargeter
    {
        private static KeypointPoint ResolveHead(
            CocoKeypoints coco,
            KeypointPoint hips,
            KeypointPoint shoulders,
            KeypointPoint neck,
            BodyBasis bodyBasis,
            float scale,
            VisionPoseRetargetingOptions options)
        {
            KeypointPoint fallback = Lerp(hips, shoulders, 1.25f);
            if (!TryAverageFace(coco, out KeypointPoint face))
                return fallback;

            float influence = HeadInfluence(face, options);
            Vector2 neckLocal = bodyBasis.ToLocal(neck.point);
            Vector2 fallbackLocal = bodyBasis.ToLocal(fallback.point);
            Vector2 faceLocal = bodyBasis.ToLocal(face.point);
            Vector2 outputLocal = Vector2.Lerp(fallbackLocal, faceLocal, influence);
            Vector2 rawHeadOffset = outputLocal - neckLocal;
            Vector2 headOffset = rawHeadOffset;

            headOffset.x = Mathf.Clamp(headOffset.x, -scale * 0.38f, scale * 0.38f);
            headOffset.y = Mathf.Clamp(headOffset.y, scale * 0.08f, scale * 0.58f);
            bool constrained = (headOffset - rawHeadOffset).sqrMagnitude > 0.000001f;

            return new KeypointPoint
            {
                point = bodyBasis.FromLocal(neckLocal + headOffset),
                confidence = Mathf.Clamp01(Mathf.Lerp(fallback.confidence, face.confidence, influence)),
                available = fallback.available || face.available,
                observed = face.observed && influence > 0.35f,
                predicted = constrained || fallback.predicted || face.predicted || influence < 0.35f
            };
        }

        private static bool TryAverageFace(CocoKeypoints coco, out KeypointPoint face)
        {
            face = default;
            Vector2 pointSum = Vector2.zero;
            float confidenceSum = 0f;
            float weightSum = 0f;
            int observedCount = 0;
            int predictedCount = 0;

            AccumulateFacePoint(coco.nose, 1.2f, ref pointSum, ref confidenceSum, ref weightSum, ref observedCount, ref predictedCount);
            AccumulateFacePoint(coco.leftEye, 1f, ref pointSum, ref confidenceSum, ref weightSum, ref observedCount, ref predictedCount);
            AccumulateFacePoint(coco.rightEye, 1f, ref pointSum, ref confidenceSum, ref weightSum, ref observedCount, ref predictedCount);
            AccumulateFacePoint(coco.leftEar, 0.7f, ref pointSum, ref confidenceSum, ref weightSum, ref observedCount, ref predictedCount);
            AccumulateFacePoint(coco.rightEar, 0.7f, ref pointSum, ref confidenceSum, ref weightSum, ref observedCount, ref predictedCount);

            if (weightSum <= 0f)
                return false;

            face = new KeypointPoint
            {
                point = pointSum / weightSum,
                confidence = Mathf.Clamp01(confidenceSum / weightSum),
                available = true,
                observed = observedCount > 0,
                predicted = predictedCount > 0 || observedCount == 0
            };
            return true;
        }

        private static void AccumulateFacePoint(
            KeypointPoint point,
            float sourceWeight,
            ref Vector2 pointSum,
            ref float confidenceSum,
            ref float weightSum,
            ref int observedCount,
            ref int predictedCount)
        {
            if (!point.available || point.confidence <= 0.01f)
                return;

            float weight = point.confidence * sourceWeight;
            pointSum += point.point * weight;
            confidenceSum += point.confidence * weight;
            weightSum += weight;
            if (point.observed)
                observedCount++;
            if (point.predicted)
                predictedCount++;
        }

        private static KeypointPoint StabilizeLegChild(
            KeypointPoint parent,
            KeypointPoint child,
            Vector2 fallbackLocalDirection,
            float length,
            BodyBasis bodyBasis,
            VisionPoseRetargetingOptions options)
        {
            Vector2 parentLocal = bodyBasis.ToLocal(parent.point);
            Vector2 fallbackDirection = SafeDirection(fallbackLocalDirection, Vector2.down);
            if (!child.available)
                return PredictedChild(parent, bodyBasis.FromLocal(parentLocal + fallbackDirection * length), options);

            Vector2 rawDelta = bodyBasis.ToLocal(child.point) - parentLocal;
            Vector2 constrainedDelta = ConstrainLegDelta(rawDelta, length);
            if ((constrainedDelta - rawDelta).sqrMagnitude <= 0.000001f)
                return child;

            float influence = Mathf.Clamp01(options.legKeypointInfluence) * (child.observed ? 1f : 0.35f);
            Vector2 outputDelta = Vector2.Lerp(fallbackDirection * length, constrainedDelta, influence);
            return new KeypointPoint
            {
                point = bodyBasis.FromLocal(parentLocal + outputDelta),
                confidence = Mathf.Clamp01(Mathf.Lerp(options.missingJointConfidence, child.confidence, influence)),
                available = parent.available,
                observed = child.observed && influence > 0.5f,
                predicted = true
            };
        }

        private static Vector2 ConstrainLegDelta(Vector2 delta, float expectedLength)
        {
            if (delta.sqrMagnitude <= 0.000001f)
                return Vector2.down * expectedLength;

            float maxReach = expectedLength * 1.7f;
            if (delta.magnitude > maxReach)
                delta = delta.normalized * maxReach;

            float maxLateral = expectedLength * 1.05f;
            delta.x = Mathf.Clamp(delta.x, -maxLateral, maxLateral);
            delta.y = Mathf.Min(delta.y, -expectedLength * 0.08f);
            return delta;
        }

        private static KeypointPoint PredictedChild(
            KeypointPoint parent,
            Vector2 point,
            VisionPoseRetargetingOptions options)
        {
            return new KeypointPoint
            {
                point = point,
                confidence = Mathf.Clamp01(options.missingJointConfidence),
                available = parent.available,
                observed = false,
                predicted = true
            };
        }

        private static float HeadInfluence(KeypointPoint face, VisionPoseRetargetingOptions options)
        {
            float confidenceWeight = face.observed
                ? Mathf.Lerp(0.45f, 1f, face.confidence)
                : 0.25f;
            return Mathf.Clamp01(options.headKeypointInfluence * confidenceWeight);
        }
    }
}
