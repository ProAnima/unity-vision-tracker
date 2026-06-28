# Optional Modules

The core package must remain vendor-neutral. Platform and runtime integrations live in optional packages:

```text
com.proanima.universal-vision-tracker.ar-foundation
com.proanima.universal-vision-tracker.xr
com.proanima.universal-vision-tracker.mediapipe
com.proanima.universal-vision-tracker.native
com.proanima.universal-vision-tracker.remote
```

## Rules

- Optional modules may depend on the core package.
- The core package must not depend on optional modules.
- Vendor SDK dependencies must be isolated inside optional packages.
- New cameras enter through `IVisionFrameSource`.
- New runtimes enter through `IVisionRuntimeAdapter` and `IVisionModelAdapter`.
- New tensor or service formats enter through `IVisionOutputParser`.
- Public output remains `VisionFrameResult`.

## Current Skeletons

| Package | Purpose | Extension Points |
| --- | --- | --- |
| `ar-foundation` | AR camera frame sources | `IVisionFrameSource`, `VisionFrame` |
| `xr` | XR passthrough frame sources | `IVisionFrameSource`, `VisionFrame` |
| `mediapipe` | MediaPipe runtime/parsers | `IVisionRuntimeAdapter`, `IVisionModelAdapter`, `IVisionOutputParser` |
| `native` | Native plugin runtimes | `IVisionRuntimeAdapter`, `IVisionModelAdapter`, `VisionRawModelOutput` |
| `remote` | Remote inference runtimes | `IVisionRuntimeAdapter`, `IVisionModelAdapter`, `VisionFrameResult` |

Each skeleton includes a descriptor class that reports package name, purpose, extension points, and optional external dependencies.
