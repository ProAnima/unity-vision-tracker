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

After install, confirm this menu item exists:

- `Tools/ProAnima Vision/Control Center`

Use `Control Center` as the primary entry point. The main flow is intentionally short: import/open the demo scene, choose a Quick Start preset from the dropdown, apply it, then press Play. Expert tools remain available under `Tools/ProAnima Vision/Advanced` and inside the collapsed `Advanced tools` section.

## Import Samples

Open Unity Package Manager, select `ProAnima Universal Vision Tracker`, then import:

- `Minimal Pipeline` for a compact manager bootstrap.
- `Dashboard Overlay` for UI Toolkit overlay preview without a camera or model.
- `YOLO Model Profiles` for detection, pose, and segmentation profile templates.
- `Experimental Scene` for a polished runnable WebCam dashboard scene with fit, rotation, and mirror controls.

You can also use `Control Center > Import / Open Demo Scene`; it attempts to import the Experimental Scene automatically and falls back to Package Manager instructions if Unity does not expose the sample import API.

The YOLO sample profiles are intentionally runtime-asset-free. They document parser ids, tensor schemas, labels, thresholds, and governance fields so you can add your own model asset safely.

## Apply A Quick Start Preset

Open `Tools/ProAnima Vision/Control Center`, choose one preset, then click the primary action:

- `WebCam Preview` imports/opens the demo scene and requires no model.
- `YOLO Detection + WebCam` creates or updates detection profiles, opens the demo scene, and wires the camera pipeline.
- `YOLO Pose + WebCam` creates or updates pose profiles and skeleton overlay wiring.
- `YOLO Segmentation + WebCam` creates or updates segmentation profiles and mask overlay wiring.

Preset assets are written under `Assets/ProAnima Vision/Profiles`. Re-applying the same preset updates the same assets instead of creating duplicates.

## Create A Model Profile Manually

For a custom production scene, create a `VisionModelProfile` with `Control Center > Advanced tools > Model Profile Wizard`.

Recommended first pass:

1. Pick the closest template: YOLO Detection, YOLO Pose 2D, or YOLO Segmentation.
2. Assign the Unity `ModelAsset`.
3. Assign labels when the model outputs class ids.
4. Keep input size at `640` unless the model was exported for a different size.
5. Set `modelLicense` and `modelSourceUrl` before release.
6. Run `Control Center > Profile Validator`.

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

Use `Control Center > Advanced tools > Setup Wizard`.

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
- Keep CPU for first-run validation. Try `GPUCompute` or `GPUPixel` only after the CPU path is healthy on the target device.

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
