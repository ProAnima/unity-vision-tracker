# ProAnima Vision AR Foundation Module

Optional module for AR Foundation camera frame sources.

This package intentionally does not make the core tracker depend on AR Foundation. Real AR Foundation source implementations should be added here behind compile symbols/version defines and expose frames through `IVisionFrameSource`.

## Extension Points

- `IVisionFrameSource`
- `VisionFrame`
- `VisionPipelineProfile`

## Planned Work

- AR camera CPU image source.
- AR camera texture source.
- Projection/display transform helpers.
- Device smoke tests.
