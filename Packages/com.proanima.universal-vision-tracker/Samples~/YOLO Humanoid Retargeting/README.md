# YOLO Humanoid Retargeting Sample

This sample demonstrates the production retargeting path from a YOLO/COCO-17 pose into a canonical Unity humanoid pose and then into a Unity rig.

## What It Shows

- A runtime-created humanoid rig hierarchy with standard hips, spine, chest, neck, head, arm, hand, leg, and foot joints.
- A synthetic COCO-17 pose stream, including intermittent wrist dropout.
- `VisionCocoHumanoidPoseRetargeter` converting `VisionPose` into `VisionHumanoidPose`.
- `VisionHumanoidRigReceiver` applying the canonical pose to explicit `Transform` bindings.
- Temporal hold, prediction, and kinematic fallback so short wrist/ankle losses do not snap limbs back to bind pose.

## How To Use

Preferred path:

1. Open `Tools > ProAnima Vision > Control Center`.
2. Click `Import / Open Retargeting Demo`.
3. Enter Play Mode.

Manual path:

1. Import the sample from Package Manager.
2. Create an empty scene with a camera and light.
3. Add `ProAnimaVisionYoloHumanoidRetargetingDemo` to an empty GameObject.
4. Enter Play Mode.

The sample uses generated Unity primitives rather than an FBX so the retargeting logic can be inspected without external assets. Production rigs should use `VisionHumanoidRigReceiver` with either an `Animator` humanoid auto-binding or explicit `Transform` bindings for a Generic rig.
