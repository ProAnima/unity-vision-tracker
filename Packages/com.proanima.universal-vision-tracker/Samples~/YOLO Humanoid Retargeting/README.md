# YOLO Humanoid Retargeting Sample

This sample demonstrates the first production retargeting path from a YOLO/COCO-17 pose into a canonical Unity humanoid pose.

## What It Shows

- A runtime-created test rig with standard humanoid joints.
- A synthetic COCO-17 pose stream, including intermittent wrist dropout.
- `VisionCocoHumanoidPoseRetargeter` converting `VisionPose` into `VisionHumanoidPose`.
- Temporal hold/prediction so short keypoint losses do not snap limbs back to bind pose.

## How To Use

1. Import the sample from Package Manager.
2. Add `ProAnimaVisionYoloHumanoidRetargetingDemo` to an empty GameObject.
3. Enter Play Mode.

The sample uses generated Unity primitives rather than an FBX so the retargeting logic can be inspected without external assets. Production rigs should consume `VisionHumanoidPose` from a receiver that maps joints to `Animator` humanoid bones, generic bone transforms, or Animation Rigging targets.
