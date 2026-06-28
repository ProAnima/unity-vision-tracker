# ProAnima Universal Vision Tracker

This package contains the production Unity runtime for ProAnima Universal Vision Tracker.

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

## Layout

```text
Runtime/          Runtime assemblies and production code
Editor/           Editor-only profile tools and validation windows
Documentation~/   Package documentation excluded from asset import
Samples~/         Importable UPM samples
```

Main user documentation lives in the repository root `README.md`. Production rules live in repository root `CODEX.md`.
