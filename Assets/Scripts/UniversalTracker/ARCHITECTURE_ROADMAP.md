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
- Prototype model wrappers, direct model factories, result bridges, and old input-provider contracts have been removed from runtime code.

## Next Production Steps

1. Add health/state events to `VisionPipeline` and expose them through `UniversalTrackerManager`.
2. Add PlayMode smoke tests for the UI Toolkit overlay and manager lifecycle.
3. Add golden fixtures for pose and segmentation parsers.
4. Add parser selection diagnostics to `VisionProfileValidator`.
5. Move runtime code toward UPM layout: `Runtime/`, `Editor/`, `Samples~/`, `Documentation~/`.
6. Add optional modules for AR Foundation, XR passthrough, MediaPipe, native plugin runtimes, and remote runtimes.
7. Add performance/allocation budgets for parser, overlay, and tracking paths.

## Extension Rules

- New cameras enter through `IVisionFrameSource`.
- New model families enter through `IVisionModelAdapter`.
- New runtime backends enter through `IVisionRuntimeAdapter`.
- New tensor formats enter through `IVisionOutputParser`.
- New UI/debug surfaces consume `VisionFrameResult`.
- Core runtime must stay vendor-neutral except for isolated runtime adapters.

## Migration Impact

Scenes must be configured with `VisionPipelineProfile` or `VisionModelProfile`. Removed prototype APIs should not be referenced by new scenes, docs, tests, samples, or adapters.
