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
- Editor tooling now includes `Tools/ProAnima Vision/Profile Validator`, `Tools/ProAnima Vision/Profile Wizard`, and asset creation shortcuts for YOLO detection, pose, segmentation, and pipeline profiles.
- YOLO pose and segmentation profiles resolve to production parser contracts and have EditMode fixture coverage.
- Prototype Unity Inference smoke MonoBehaviours have been removed from runtime code.
- Prototype model wrappers, direct model factories, result bridges, and old input-provider contracts have been removed from runtime code.
- UI Toolkit dashboard receiver and overlay renderer have PlayMode smoke coverage for runtime visual tree creation and detection/pose/mask element rendering.
- Runtime and editor code now live in an embedded UPM package layout under `Packages/com.proanima.universal-vision-tracker`.
- `VisionPipelineProfile` now carries simple performance/allocation budgets for parser, overlay, tracking, and pipeline stages.
- `Tools/ProAnima Vision/Setup Wizard` creates or updates a scene tracker with profile, source, tracking, and UI Toolkit dashboard wiring.
- Package Manager samples now cover minimal profile-driven setup and dashboard overlay preview without requiring model assets.
- Getting Started documentation now describes the profile-first happy path from install to scene setup, validator use, dashboard feedback, and production checklist.
- Package Manager samples now include YOLO detection, pose, and segmentation model-profile templates with labels and parser schemas.
- Optional module package skeletons now isolate AR Foundation, XR passthrough, MediaPipe, native runtime, and remote runtime work from the core package.
- Profile validation now exposes a concise summary for setup tooling, and the UI Toolkit dashboard surfaces pipeline budget status.
- `Tools/ProAnima Vision/Profile Compatibility Inspector` summarizes runtime, parser, capability, input, and output-schema readiness for model and pipeline profiles.
- UI Toolkit dashboard status now includes source, model, runtime, budget, health, consecutive errors, and last error details.

## Next Production Steps

1. Add screenshot-level PlayMode checks for dashboard layout across common viewport sizes.
2. Add scene lifecycle smoke tests for `UniversalTrackerManager` startup, stop, failure, and recovery paths.
3. Add runnable model-profile samples once lightweight model assets with clear redistribution licenses are available.
4. Implement first real optional module integration behind compile symbols.
5. Add richer model-profile samples for additional export layouts once lightweight model assets with clear redistribution licenses are available.

## Extension Rules

- New cameras enter through `IVisionFrameSource`.
- New model families enter through `IVisionModelAdapter`.
- New runtime backends enter through `IVisionRuntimeAdapter`.
- New tensor formats enter through `IVisionOutputParser`.
- New UI/debug surfaces consume `VisionFrameResult`.
- Core runtime must stay vendor-neutral except for isolated runtime adapters.

## Migration Impact

Scenes must be configured with `VisionPipelineProfile` or `VisionModelProfile`. Removed prototype APIs should not be referenced by new scenes, docs, tests, samples, or adapters.
