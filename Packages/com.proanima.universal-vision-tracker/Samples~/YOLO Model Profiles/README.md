# YOLO Model Profiles

This sample contains production-oriented `VisionModelProfile` templates for common YOLO tasks:

- `YoloDetectionCocoProfile.asset`
- `YoloPose2DPersonProfile.asset`
- `YoloSegmentationCocoProfile.asset`

These profiles are templates, not runnable model packages. They intentionally leave `modelAsset` empty so the package does not distribute large weights or inherit third-party model licenses.

## Use

1. Import this sample from Package Manager.
2. Move or duplicate the profile you need into your project settings folder.
3. Assign a Unity `ModelAsset` converted from the matching ONNX export.
4. Assign or replace the labels file.
5. Open `Tools/ProAnima Vision/Profile Validator`.
6. Add the validated profile to a `VisionPipelineProfile`.

## Included Schemas

`YoloDetectionCocoProfile.asset`

- parser id: `yolo.detection.rows`
- output tensor: `output0` `[1, 84, 8400]`
- capabilities: detection

`YoloPose2DPersonProfile.asset`

- parser id: `yolo.pose2d.rows`
- output tensor: `output0` `[1, 56, 8400]`
- capabilities: detection, human detection, pose 2D

`YoloSegmentationCocoProfile.asset`

- parser id: `yolo.segmentation.rows`
- output tensors: `output0` `[1, 116, 8400]`, `proto` `[1, 32, 160, 160]`
- capabilities: detection, segmentation

If your export uses different tensor names or shapes, update the profile instead of changing parser code.
