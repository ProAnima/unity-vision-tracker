# ProAnima Vision Remote Runtime Module

Optional module for remote inference runtimes.

Remote integrations should convert service responses into `VisionFrameResult` or `VisionRawModelOutput` plus parser contracts without changing the core pipeline.

## Extension Points

- `IVisionRuntimeAdapter`
- `IVisionModelAdapter`
- `VisionFrameResult`
- `VisionHealthStatus`

## Planned Work

- Remote endpoint runtime adapter.
- Timeout and degraded-health policy.
- Response parser fixtures.
