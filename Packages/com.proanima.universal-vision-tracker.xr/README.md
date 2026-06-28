# ProAnima Vision XR Module

Optional module for XR passthrough and headset camera frame sources.

Vendor SDK implementations should live here, behind compile symbols and capability checks. The core package should continue to consume only `IVisionFrameSource` and `VisionFrame`.

## Extension Points

- `IVisionFrameSource`
- `VisionFrame`
- `VisionPipelineProfile`

## Planned Work

- XR passthrough texture source.
- Per-device transform metadata.
- Platform-specific availability checks.
