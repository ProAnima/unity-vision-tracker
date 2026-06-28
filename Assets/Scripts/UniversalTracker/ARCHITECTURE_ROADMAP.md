# Architecture Roadmap

This roadmap describes the production architecture after removal of the prototype runtime path.

## Current Spine

```text
IVisionFrameSource
  -> VisionPipeline
    -> IVisionRuntimeAdapter
      -> IVisionOutputParser
        -> VisionFrameResult
          -> IOutputReceiver
```

The public result surface is `VisionFrameResult` with `VisionDetection`, `VisionPose`, `VisionMask`, and `VisionClassification`.

Configuration is profile-first:

- `VisionPipelineProfile` selects models and runtime policy.
- `VisionModelProfile` describes model family, runtime kind, schemas, capabilities, thresholds, labels, and governance metadata.
- `VisionProfileValidator` protects runtime startup from incomplete profiles.

## Completed Productionization Increments

- `UniversalTrackerManager` is now a thin MonoBehaviour facade over `VisionPipeline`.
- Frame ingestion uses production `IVisionFrameSource` implementations for webcam, texture, render texture, Unity camera, and video.
- Model execution uses `IVisionRuntimeAdapter` and `IVisionModelAdapter`.
- Unity Inference execution flows through `UnityInferenceRawOutputProvider`, `VisionRawModelOutput`, and parser contracts.
- YOLO-specific handling lives in `YoloModelAdapter` and output parsers.
- Output receivers consume `VisionFrameResult` directly.
- NMS and tracking operate on `VisionDetection`.
- Runtime lifecycle is exposed through `VisionHealthStatus` and started/stopped/degraded/failed/recovered events.
- Model profile validation reports parser selection, missing parser ids, unsupported profile/parser combinations, and output semantic warnings before runtime startup.
- UI Toolkit dashboard construction is split into a builder/theme helper so the receiver focuses on binding runtime state to UI updates.
- UI Toolkit dashboard state binding is split into focused stats and result-list binders with EditMode coverage.
- `UniversalTrackerManager` delegates frame-source creation, profile resolution, output receiver routing, tracking, health event dispatch, and profile-validation logging to focused runtime services.
- Frame source implementations are split into focused runtime files so new camera/video adapters can be added without extending a monolithic source module.
- Editor tooling now includes `Tools/ProAnima Vision/Profile Validator` plus asset creation shortcuts for YOLO detection model profiles and pipeline profiles from selected models.
- Prototype Unity Inference smoke MonoBehaviours have been removed from runtime code.
- Prototype model wrappers, direct model factories, result bridges, and old input-provider contracts have been removed from runtime code.

## Next Production Steps

1. Add PlayMode smoke tests for the UI Toolkit overlay and manager lifecycle.
2. Add golden fixtures for pose and segmentation parsers.
3. Add profile creation wizards and presets for common YOLO detection/pose/segmentation profiles.
4. Move runtime code toward UPM layout: `Runtime/`, `Editor/`, `Samples~/`, `Documentation~/`.
5. Add optional modules for AR Foundation, XR passthrough, MediaPipe, native plugin runtimes, and remote runtimes.
6. Add performance/allocation budgets for parser, overlay, and tracking paths.

## Extension Rules

- New cameras enter through `IVisionFrameSource`.
- New model families enter through `IVisionModelAdapter`.
- New runtime backends enter through `IVisionRuntimeAdapter`.
- New tensor formats enter through `IVisionOutputParser`.
- New UI/debug surfaces consume `VisionFrameResult`.
- Core runtime must stay vendor-neutral except for isolated runtime adapters.

## Migration Impact

Scenes must be configured with `VisionPipelineProfile` or `VisionModelProfile`. Removed prototype APIs should not be referenced by new scenes, docs, tests, samples, or adapters.
