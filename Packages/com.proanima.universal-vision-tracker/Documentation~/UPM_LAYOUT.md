# UPM Layout

This package is the production home for ProAnima Universal Vision Tracker.

```text
Packages/com.proanima.universal-vision-tracker/
  package.json
  Runtime/
    UniversalTracker.Runtime.asmdef
    Core/
    OutputReceivers/
    Processing/
    Tracking/
    Visualization/
  Editor/
    UniversalTracker.Editor.asmdef
  Documentation~/
  Samples~/
```

## Rules

- Runtime code must stay under `Runtime/`.
- Editor-only code must stay under `Editor/`.
- Samples must stay under `Samples~/` and must not be required for runtime compilation.
- Documentation that should not be imported as Unity assets belongs in `Documentation~/`.
- Optional integrations such as AR Foundation, XR passthrough, MediaPipe, native plugin runtimes, and remote runtimes should be added as separate optional modules instead of making the core runtime depend on vendor packages.
- Public integrations must continue through `IVisionFrameSource`, `IVisionRuntimeAdapter`, `IVisionModelAdapter`, `IVisionOutputParser`, and `IOutputReceiver`.
