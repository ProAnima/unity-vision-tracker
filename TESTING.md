# Testing

This project uses Unity Test Framework EditMode tests for production-core code that can be validated without cameras, devices, or Play Mode.

## EditMode Tests

Current coverage starts with:

- `VisionFrameResult` result-count and pose keypoint behavior;
- `VisionPerformanceStats` stage totals;
- `VisionImageTransform` stretch, letterbox, normalized mapping, and source/model round trips.
- `VisionResultAdapter` compatibility mapping from legacy `InferenceResult` to `VisionFrameResult`.
- `NMSProcessor` IoU, class-aware suppression, null input, and confidence ordering behavior.
- `IOUTracker` and `SORTTracker` track ID stability, missed-frame lifecycle, reset behavior, and confirmation behavior.

Run from PowerShell:

```powershell
& 'D:\Unity\6000.3.15f1\Editor\Unity.exe' `
  -batchmode `
  -nographics `
  -projectPath 'D:\Projects\ProAnimaStudio\pas-UCT' `
  -runTests `
  -testPlatform EditMode `
  -testResults 'D:\Projects\ProAnimaStudio\pas-UCT\Temp\editmode-test-results.xml' `
  -logFile 'D:\Projects\ProAnimaStudio\pas-UCT\Temp\editmode-test.log'
```

Do not pass `-quit` with `-runTests`; Unity Test Runner exits on its own after the run.

On Unity `6000.3.15f1`, the log may report that results were saved to both the requested path and Unity's LocalLow default path. If the requested XML is missing, check:

```text
C:\Users\<user>\AppData\LocalLow\DefaultCompany\pas-UCT\TestResults.xml
```

Expected current baseline:

```text
EditMode: 46 tests, 46 passed, 0 failed
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
| Legacy-to-production bridge | EditMode unit tests | Started |
| NMS/postprocess math | EditMode unit tests | Started |
| Tracking lifecycle | EditMode unit tests | Started |
| Model profile validation | EditMode unit tests | Planned |
| Parser output schemas | Golden fixture tests | Planned |
| Frame sources | EditMode + PlayMode tests | Planned |
| WebCam/RenderTexture path | PlayMode/manual device tests | Planned |
| Unity Inference runtime smoke | PlayMode/batchmode tests | Planned |
| UI overlay mapping | PlayMode screenshot tests | Planned |
| Performance/allocation budgets | Performance tests | Planned |
| AR/XR sources | Platform integration tests | Planned |
