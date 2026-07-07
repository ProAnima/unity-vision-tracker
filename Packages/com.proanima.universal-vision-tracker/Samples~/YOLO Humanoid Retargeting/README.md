# YOLO Humanoid Retargeting Sample

This sample demonstrates the production retargeting path from a YOLO/COCO-17 pose into a canonical Unity humanoid pose and then into a Unity rig.

## What It Shows

- A UI Toolkit split view: left source preview with source controls, person box, COCO-17 bones, and keypoints; right retargeted Unity rig.
- Runtime source selection for Camera, Video, and an explicit Synthetic fixture.
- WebCam device selection by device name, plus refresh and next-camera controls for machines where the target camera is not index 0.
- Video playlist selection through `VisionVideoPlaylistSource`, reusing the same clip/URL playlist path as the experimental scene sample.
- Real YOLO pose detections through `UniversalTrackerManager` when `YoloPose2DProfile` is assigned.
- A runtime-created humanoid rig hierarchy with standard hips, spine, chest, neck, head, arm, hand, leg, and foot joints.
- A synthetic COCO-17 fixture mode, including intermittent wrist dropout, for retargeting-only inspection.
- `VisionCocoHumanoidPoseRetargeter` converting `VisionPose` into `VisionHumanoidPose`.
- `VisionHumanoidRigReceiver` applying the canonical pose to explicit `Transform` bindings.
- Temporal hold, prediction, and kinematic fallback so short wrist/ankle losses do not snap limbs back to bind pose.

## How To Use

Preferred path:

1. Open `Tools > ProAnima Vision > Control Center`.
2. Click `Import / Open Retargeting Demo`.
3. Enter Play Mode and choose `Camera`, `Video`, or `Synthetic` in the left source panel.

Manual path:

1. Import the sample from Package Manager.
2. Create an empty scene with a camera and light.
3. Add `ProAnimaVisionYoloHumanoidRetargetingDemo` to an empty GameObject.
4. Enter Play Mode.

The sample uses generated Unity primitives rather than an FBX so the retargeting logic can be inspected without external assets. In Camera and Video modes, the left preview and the rig are driven by the real `VisionFrameResult` emitted by `UniversalTrackerManager`; if YOLO does not detect a pose, the rig does not animate from fake data. Synthetic mode is deliberately separate so dropout filtering and retargeted bones can still be inspected without a model or camera. Camera and video preview frames are routed through the package frame-source contracts instead of sample-only source code. Production rigs should use `VisionHumanoidRigReceiver` with either an `Animator` humanoid auto-binding or explicit `Transform` bindings for a Generic rig.
