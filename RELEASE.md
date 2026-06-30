# Release Checklist

Use this checklist before tagging a public package release.

## Versioning

- Update `Packages/com.proanima.universal-vision-tracker/package.json`.
- Update `Packages/com.proanima.universal-vision-tracker/CHANGELOG.md`.
- Confirm the `package.json` version matches the release tag.
- Use preview tags for unstable APIs, for example `v0.1.0-preview.1`.
- Use stable tags only after installation, samples, and runtime smoke checks pass.

## Validation

Run the package validation gate:

```powershell
python scripts/validate_upm_package.py
```

Run Unity tests:

```powershell
& 'D:\Unity\6000.5.1f1\Editor\Unity.exe' `
  -batchmode `
  -projectPath 'D:\Projects\ProAnimaStudio\pas-UCT' `
  -runTests `
  -testPlatform EditMode `
  -testResults 'D:\Projects\ProAnimaStudio\pas-UCT\Temp\EditModeResults.xml' `
  -logFile 'D:\Projects\ProAnimaStudio\pas-UCT\Temp\EditModeTests.log'
```

```powershell
& 'D:\Unity\6000.5.1f1\Editor\Unity.exe' `
  -batchmode `
  -projectPath 'D:\Projects\ProAnimaStudio\pas-UCT' `
  -runTests `
  -testPlatform PlayMode `
  -testResults 'D:\Projects\ProAnimaStudio\pas-UCT\Temp\PlayModeResults.xml' `
  -logFile 'D:\Projects\ProAnimaStudio\pas-UCT\Temp\PlayModeTests.log'
```

Expected baseline:

```text
EditMode: 138 tests, 138 passed, 0 failed
PlayMode: 3 tests, 3 passed, 0 failed
```

## UPM Install Smoke

Before a public release, verify a clean Unity 6 project can install:

```text
https://github.com/ProAnima/unity-vision-tracker.git?path=/Packages/com.proanima.universal-vision-tracker
```

Then import these samples from Package Manager:

- `Experimental Scene`
- `YOLO Model Profiles`
- `Minimal Pipeline`
- `Dashboard Overlay`

Confirm the imported sample scene compiles and the webcam dashboard can start.

## Model And License Policy

- Do not bundle `.onnx`, `.sentis`, `.tflite`, `.pt`, `.pth`, `.weights`, or `.engine` files in the core package.
- Keep YOLO model weights outside the MIT package unless redistribution terms are explicitly approved.
- Keep model license and source metadata in every `VisionModelProfile` template.
- Keep optional vendor integrations in separate packages or modules.

## GitHub Release

- Create a signed or annotated tag matching the package version.
- Include installation URL, Unity version baseline, dependency versions, test baseline, and known limitations.
- Attach no model weights to the core package release unless license review is complete.
