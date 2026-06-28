# Testing

This project uses Unity Test Framework EditMode tests for production-core code and PlayMode smoke tests for scene-facing UI/runtime behavior.

## EditMode Tests

Current coverage starts with:

- `VisionFrameResult` result-count and pose keypoint behavior;
- `VisionPerformanceStats` stage totals;
- `VisionImageTransform` stretch, letterbox, normalized mapping, and source/model round trips.
- `NMSProcessor` IoU, class-aware suppression, null input, and confidence ordering behavior.
- `IOUTracker` and `SORTTracker` track ID stability, missed-frame lifecycle, reset behavior, and confirmation behavior.
- `VisionProfileValidator` model and pipeline profile validation behavior.
- `VisionAdapterRegistry` adapter registration, duplicate protection, resolution, and runtime creation behavior.
- `VisionHealthStatus` lifecycle transitions for start, degradation, recovery, stop, and failure.
- Parser selection diagnostics for explicit parser ids, unsupported profile combinations, and missing parser coverage.
- Production `IVisionFrameSource` implementations for texture, render texture, Unity camera, and video contract behavior.
- `UnityInferenceRuntimeAdapter` and `YoloModelAdapter` runtime creation, processing, and disposal behavior through fakes.
- Parser contracts and YOLO detection golden row fixtures.
- Parser registry and raw-output runtime flow through `UnityInferenceRuntimeAdapter`.
- Unity Inference raw output provider construction and missing-asset failure behavior.
- UI Toolkit dashboard receiver and overlay renderer PlayMode smoke behavior.
- Performance/allocation budget policy for parser, overlay, tracking, and pipeline stages.
- Setup Wizard scene configuration behavior.
- Package sample manifest and file layout, including YOLO model-profile samples.
- Optional module descriptor and package isolation rules.
- Profile validation summary and dashboard budget status behavior.
- Profile compatibility summary behavior for parser, runtime, capability, input, and output schemas.
- Dashboard runtime context binding for source, model, runtime, and last error status.
- Editor tooling entry points for Control Center and pipeline profile creation.

Run from PowerShell:

```powershell
& 'D:\Unity\6000.5.1f1\Editor\Unity.exe' `
  -batchmode `
  -nographics `
  -projectPath 'D:\Projects\ProAnimaStudio\pas-UCT' `
  -runTests `
  -testPlatform EditMode `
  -testResults 'D:\Projects\ProAnimaStudio\pas-UCT\Temp\editmode-test-results.xml' `
  -logFile 'D:\Projects\ProAnimaStudio\pas-UCT\Temp\editmode-test.log'
```

Do not pass `-quit` with `-runTests`; Unity Test Runner exits on its own after the run.

On Unity `6000.5.1f1`, the log may report that results were saved to both the requested path and Unity's LocalLow default path. If the requested XML is missing, check:

```text
C:\Users\<user>\AppData\LocalLow\DefaultCompany\pas-UCT\TestResults.xml
```

Expected current baseline:

```text
EditMode: 121 tests, 121 passed, 0 failed
PlayMode: 2 tests, 2 passed, 0 failed
```

Run PlayMode smoke tests from PowerShell:

```powershell
& 'D:\Unity\6000.5.1f1\Editor\Unity.exe' `
  -batchmode `
  -projectPath 'D:\Projects\ProAnimaStudio\pas-UCT' `
  -runTests `
  -testPlatform PlayMode `
  -testResults 'D:\Projects\ProAnimaStudio\pas-UCT\Temp\playmode-test-results.xml' `
  -logFile 'D:\Projects\ProAnimaStudio\pas-UCT\Temp\playmode-test.log'
```

## GitHub Actions

The repository includes a GitHub Actions workflow:

```text
.github/workflows/unity-editmode-tests.yml
```

It uses GameCI Unity Test Runner to run EditMode tests on push and pull requests.

Required repository secrets:

```text
UNITY_LICENSE
UNITY_EMAIL
UNITY_PASSWORD
```

For organization repositories, configure these secrets in GitHub repository settings before relying on CI.

## Production Test Matrix

Current automated tests cover the pure runtime core. A production-ready release should expand coverage into these layers:

| Layer | Test Type | Status |
| --- | --- | --- |
| Result API | EditMode unit tests | Started |
| Coordinate mapping | EditMode unit tests | Started |
| Runtime dashboard geometry | EditMode unit tests | Started |
| Plugin/adapters contracts | EditMode unit tests | Started |
| Vision pipeline orchestration | EditMode unit tests | Started |
| NMS/postprocess math | EditMode unit tests | Started |
| Tracking lifecycle | EditMode unit tests | Started |
| Model profile validation | EditMode unit tests | Started |
| Parser output schemas | Golden fixture tests | Started |
| Frame sources | EditMode + PlayMode tests | Started |
| WebCam/RenderTexture path | PlayMode/manual device tests | Planned |
| Unity Inference runtime smoke | PlayMode/batchmode tests | Planned |
| UI overlay mapping | PlayMode screenshot tests | Planned |
| Performance/allocation budgets | Performance tests | Planned |
| AR/XR sources | Platform integration tests | Planned |
