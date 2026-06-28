# ProAnima Vision Native Runtime Module

Optional module for native plugin inference runtimes.

Native integrations should expose stable managed adapters through `IVisionRuntimeAdapter` and avoid leaking vendor APIs into the core package.

## Extension Points

- `IVisionRuntimeAdapter`
- `IVisionModelAdapter`
- `VisionRawModelOutput`
- `IVisionOutputParser`

## Planned Work

- Native runtime lifecycle wrapper.
- Platform availability checks.
- Raw tensor bridge tests.
