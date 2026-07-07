using System;
using UnityEngine;

namespace UniversalTracker.Core
{
    public sealed class VisionPoseTemporalFilter
    {
        private KeypointState[] states = Array.Empty<KeypointState>();
        private int previousPersonId = int.MinValue;

        public VisionKeypoint[] Update(VisionPose pose, float deltaTime, VisionPoseRetargetingOptions options)
        {
            options ??= VisionPoseRetargetingOptions.Default;
            VisionKeypoint[] input = pose.keypoints ?? Array.Empty<VisionKeypoint>();
            EnsureState(input.Length, pose.personId);

            var output = new VisionKeypoint[input.Length];
            float dt = Mathf.Max(0.0001f, deltaTime);
            for (int i = 0; i < input.Length; i++)
            {
                output[i] = UpdateKeypoint(input[i], i, dt, options);
            }

            return output;
        }

        public void Reset()
        {
            states = Array.Empty<KeypointState>();
            previousPersonId = int.MinValue;
        }

        private void EnsureState(int count, int personId)
        {
            if (states.Length == count && previousPersonId == personId)
                return;

            states = new KeypointState[count];
            previousPersonId = personId;
        }

        private VisionKeypoint UpdateKeypoint(
            VisionKeypoint input,
            int index,
            float deltaTime,
            VisionPoseRetargetingOptions options)
        {
            KeypointState state = states[index];
            bool observed = input.isVisible && input.confidence >= options.keypointConfidenceThreshold;

            if (observed)
                return UpdateObserved(input, index, deltaTime, options, state);

            return UpdateMissing(input, index, deltaTime, options, state);
        }

        private VisionKeypoint UpdateObserved(
            VisionKeypoint input,
            int index,
            float deltaTime,
            VisionPoseRetargetingOptions options,
            KeypointState state)
        {
            Vector2 position = input.normalizedPosition;
            if (state.hasValue)
            {
                float blend = Mathf.Clamp01(1f - options.smoothing);
                position = Vector2.Lerp(state.position, input.normalizedPosition, blend);
                state.velocity = (position - state.position) / deltaTime;
            }

            state.position = position;
            state.confidence = input.confidence;
            state.missingSeconds = 0f;
            state.hasValue = true;
            state.predicted = false;
            states[index] = state;

            input.normalizedPosition = position;
            input.confidence = state.confidence;
            input.isVisible = true;
            return input;
        }

        private VisionKeypoint UpdateMissing(
            VisionKeypoint input,
            int index,
            float deltaTime,
            VisionPoseRetargetingOptions options,
            KeypointState state)
        {
            if (!state.hasValue)
            {
                input.isVisible = false;
                input.confidence = 0f;
                return input;
            }

            state.missingSeconds += deltaTime;
            if (state.missingSeconds > options.dropoutHoldSeconds)
            {
                state.confidence = Mathf.MoveTowards(state.confidence, 0f, deltaTime * 4f);
                states[index] = state;
                input.isVisible = state.confidence > 0.01f;
                input.confidence = state.confidence;
                input.normalizedPosition = state.position;
                return input;
            }

            state.position += state.velocity * deltaTime;
            state.confidence = Mathf.Min(Mathf.Clamp01(state.confidence * 0.9f), options.keypointConfidenceThreshold * 0.99f);
            state.predicted = true;
            states[index] = state;

            input.normalizedPosition = state.position;
            input.confidence = state.confidence;
            input.isVisible = true;
            return input;
        }

        private struct KeypointState
        {
            public Vector2 position;
            public Vector2 velocity;
            public float confidence;
            public float missingSeconds;
            public bool hasValue;
            public bool predicted;
        }
    }
}
