# Getting Started

This guide is the shortest production path from a clean Unity project to a profile-driven vision tracker.

## Requirements

- Unity 6 or newer.
- `com.unity.ai.inference` `2.6.1`.
- A Unity `ModelAsset` converted from an ONNX model.
- Labels as a `TextAsset` when the parser expects class names.

The core package does not ship model weights. Keep large model and media assets in Git LFS and verify the model license before distributing an app.

## Install

Use one of these paths:

1. Open this repository as a Unity project.
2. Or add the embedded package from `Packages/com.proanima.universal-vision-tracker` to another Unity project.

After install, confirm these menu items exist:

- `Tools/ProAnima Vision/Control Center`
- `Tools/ProAnima Vision/Setup Wizard`
- `Tools/ProAnima Vision/Profile Wizard`
- `Tools/ProAnima Vision/Profile Validator`

Use `Control Center` as the primary entry point. The other tools remain available directly for faster expert workflows.

## Import Samples

Open Unity Package Manager, select `ProAnima Universal Vision Tracker`, then import:

- `Minimal Pipeline` for a compact manager bootstrap.
- `Dashboard Overlay` for UI Toolkit overlay preview without a camera or model.
- `YOLO Model Profiles` for detection, pose, and segmentation profile templates.
- `Experimental Scene` for a polished runnable dashboard scene with synthetic detections, pose, and masks.

The YOLO sample profiles are intentionally runtime-asset-free. They document parser ids, tensor schemas, labels, thresholds, and governance fields so you can add your own model asset safely.

## Create A Model Profile

For a production scene, create a `VisionModelProfile` with `Tools/ProAnima Vision/Profile Wizard`.

Recommended first pass:

1. Pick the closest template: YOLO Detection, YOLO Pose 2D, or YOLO Segmentation.
2. Assign the Unity `ModelAsset`.
3. Assign labels when the model outputs class ids.
4. Keep input size at `640` unless the model was exported for a different size.
5. Set `modelLicense` and `modelSourceUrl` before release.
6. Run `Tools/ProAnima Vision/Profile Validator`.

Validator warnings should be treated as release blockers unless they are documented sample placeholders such as a missing model asset.

## Create A Pipeline Profile

Create a `VisionPipelineProfile` and add one or more `VisionModelProfile` assets.

Use the pipeline profile for:

- active model selection;
- target FPS;
- tracking policy;
- debug overlay policy;
- recoverable error budget;
- parser, tracking, overlay, and pipeline performance budgets.

Keep one default model selected and make fallback models explicit.

## Setup A Scene

Use `Tools/ProAnima Vision/Setup Wizard`.

Recommended scene setup:

1. Assign the `VisionPipelineProfile`.
2. Choose the frame source type.
3. Enable tracking only when stable track ids are needed.
4. Enable UI Toolkit dashboard during development.
5. Click `Create Or Update Scene Tracker`.

The wizard creates or updates a thin `UniversalTrackerManager` facade. Runtime orchestration stays in `VisionPipeline`.

## Run

Press Play and watch:

- health state;
- FPS and frame count;
- active model summary;
- detections, poses, masks, and labels;
- budget status;
- last error, if any.

The dashboard consumes `VisionFrameResult`, so it should work the same way for Unity Inference, remote runtimes, native runtimes, and future adapters.

## Troubleshooting

If startup fails:

- Run the Profile Validator first.
- Confirm `modelAsset` is assigned for runtime execution.
- Confirm parser id matches the output schema.
- Confirm texture or camera source reports a non-zero size.
- Check that labels count matches the expected classes.
- Use CPU or `GPUPixel` if a GPU compute backend is not available on the target device.

If overlay geometry looks wrong:

- Confirm source width and height.
- Confirm the model input preserves aspect ratio only when the export expects letterboxing.
- Check dashboard source/view metrics.
- Keep all visualization consumers on `VisionFrameResult`; do not consume raw runtime tensors in production UI.

## Production Checklist

Before shipping an app:

- Profile Validator passes with `requireRuntimeAsset` enabled.
- Runtime health reaches `Running` and recovers from transient source/runtime errors.
- Target device stays within pipeline and overlay performance budgets.
- Debug per-frame logs are disabled.
- Model license and source URL are documented.
- Large model assets are tracked by Git LFS.
- Scene uses `VisionPipelineProfile` instead of direct prototype fields.
