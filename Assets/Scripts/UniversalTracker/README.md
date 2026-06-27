# Universal Tracker Runtime

This folder contains the production Unity runtime for ProAnima Universal Vision Tracker.

The supported architecture is:

```text
IVisionFrameSource
  -> VisionPipeline
    -> IVisionRuntimeAdapter
      -> IVisionOutputParser
        -> VisionFrameResult
          -> IOutputReceiver
```

Use `VisionPipelineProfile` and `VisionModelProfile` assets for setup. Runtime output must flow through `VisionFrameResult`, `VisionDetection`, `VisionPose`, `VisionMask`, and `VisionClassification`.

Removed prototype contracts are not part of the public API. New integrations should enter through frame sources, runtime adapters, output parsers, profiles, and receivers.

Main user documentation lives in the repository root `README.md`. Production rules live in `CODEX.md`.
