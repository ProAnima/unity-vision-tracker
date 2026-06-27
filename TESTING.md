# Testing

This project uses Unity Test Framework EditMode tests for production-core code that can be validated without cameras, devices, or Play Mode.

## EditMode Tests

Current coverage starts with:

- `VisionFrameResult` result-count and pose keypoint behavior;
- `VisionPerformanceStats` stage totals;
- `VisionImageTransform` stretch, letterbox, normalized mapping, and source/model round trips.

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
EditMode: 11 tests, 11 passed, 0 failed
```

