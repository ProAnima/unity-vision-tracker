# ProAnima Vision MediaPipe Module

Optional module for MediaPipe-style runtime adapters and parsers.

The core tracker should continue to see MediaPipe as `IVisionRuntimeAdapter`, `IVisionModelAdapter`, and `IVisionOutputParser` implementations.

## Extension Points

- `IVisionRuntimeAdapter`
- `IVisionModelAdapter`
- `IVisionOutputParser`
- `VisionFrameResult`

## Planned Work

- MediaPipe pose runtime adapter.
- Hand/face landmark parser contracts.
- Golden fixture tests for normalized landmarks.
