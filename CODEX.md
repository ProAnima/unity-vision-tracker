# ProAnima Universal Vision Tracker Production Contract

This file is the single source of truth for Codex, Claude, Cursor agents, and human contributors working on this repository.

## Product Direction

ProAnima Universal Vision Tracker is a Unity-first computer vision runtime, not a single YOLO demo.

The production architecture must stay model-agnostic:

```text
Frame Source
  -> Preprocess
    -> Runtime Adapter
      -> Output Parser
        -> Postprocess
          -> Tracking
            -> VisionFrameResult
              -> Receivers / Integrations
```

YOLO is one adapter family. MediaPipe, SAM-style segmentation, depth models, native plugins, remote inference, AR Foundation, and XR passthrough must be able to join without rewriting the public API.

## Current Architectural Spine

Prefer these contracts for all new work:

- `VisionFrame` for source frame metadata.
- `VisionFrameResult` for public results.
- `VisionDetection`, `VisionPose`, `VisionKeypoint`, `VisionMask`, `VisionClassification` for result data.
- `VisionModelProfile` for model/runtime metadata.
- `VisionModelCapability` for feature routing.
- `IVisionFrameSource` for camera/texture/video/AR/XR input.
- `IVisionRuntimeAdapter` for Unity Inference Engine, MediaPipe, native, remote, mock, and future runtimes.
- `IVisionModelAdapter` for model-family adapters.
- `VisionImageTransform` and `VisionDashboardGeometry` for coordinate mapping.

Removed prototype contracts are not part of the production API. Do not reintroduce prototype input providers, model factories, direct model wrappers, or result bridges when adding new features.

## Unity Version Policy

Primary baseline:

- Unity `6000.5.1f1`
- Unity Inference Engine `2.6.1`

Compatibility goal:

- Keep runtime code clean for Unity 6.x where possible.
- Avoid APIs that lock the core package to one minor Unity version unless wrapped behind adapters or version defines.
- Optional integrations such as AR Foundation, XR passthrough, MediaPipe, or vendor SDKs must live behind optional modules/adapters and compile symbols.
- Core runtime must not require platform vendor packages.

When using Unity-specific APIs:

- Prefer `UnityEngine`, `Unity.InferenceEngine`, UI Toolkit, and standard package APIs already present in the project.
- Avoid Editor-only APIs in runtime assemblies.
- Keep PlayMode/device-only behavior behind interfaces so EditMode tests can cover the pure logic.

## Production Quality Rules

Every production change must preserve these properties:

- Public outputs are stable and model-agnostic.
- Coordinate transforms are explicit and testable.
- Runtime paths handle nulls, invalid sizes, unavailable cameras, and failed model init.
- Debug UI must be informative but not required for core runtime operation.
- Existing scenes should be migrated to profile-based setup through `VisionPipelineProfile` and `VisionModelProfile`.
- New model/camera/runtime support must enter through adapters, profiles, and schemas.
- Errors should be actionable and tied to `VisionError` / health-state concepts where possible.

## Class And File Size Limits

Use these limits as review gates:

- Target class size: under 250 lines.
- Soft maximum class size: 400 lines.
- Hard maximum class size: 600 lines unless the file is generated and explicitly documented.
- Target method size: under 40 lines.
- Soft maximum method size: 80 lines.
- Keep MonoBehaviours thin. Move pure logic into testable classes/structs.
- Avoid large manager classes absorbing responsibilities. Add focused services/adapters instead.
- Do not add abstractions just to add abstractions; add them when they define a real extension point or reduce real duplication.

Existing large files should be split when touched for substantive production work.

## Testing Requirements

Current baseline:

```text
EditMode: 120 tests, 120 passed, 0 failed
PlayMode: 2 tests, 2 passed, 0 failed
```

Run EditMode tests after code changes:

```powershell
& 'D:\Unity\6000.5.1f1\Editor\Unity.exe' `
  -batchmode `
  -projectPath 'D:\Projects\ProAnimaStudio\pas-UCT' `
  -runTests `
  -testPlatform EditMode `
  -testResults 'D:\Projects\ProAnimaStudio\pas-UCT\Temp\EditModeResults.xml' `
  -logFile 'D:\Projects\ProAnimaStudio\pas-UCT\Temp\EditModeTests.log'
```

Run PlayMode smoke tests after UI Toolkit, lifecycle, or scene-facing runtime changes:

```powershell
& 'D:\Unity\6000.5.1f1\Editor\Unity.exe' `
  -batchmode `
  -projectPath 'D:\Projects\ProAnimaStudio\pas-UCT' `
  -runTests `
  -testPlatform PlayMode `
  -testResults 'D:\Projects\ProAnimaStudio\pas-UCT\Temp\PlayModeResults.xml' `
  -logFile 'D:\Projects\ProAnimaStudio\pas-UCT\Temp\PlayModeTests.log'
```

Important runner note:

- Do not add `-quit`; Unity Test Runner exits itself in this project.
- If the requested XML is missing, check `C:\Users\Ян\AppData\LocalLow\DefaultCompany\pas-UCT\TestResults.xml`.
- After asmdef or script import changes, Unity may do a compile-only pass. Re-run tests until the XML count reflects new tests.
- Remove `Temp\UnityLockfile` only when no Unity process is running.

Test coverage expectations:

- New coordinate math requires EditMode tests.
- New adapters require capability/schema/bridge tests without real model assets where possible.
- New parsers require golden fixture tests before production release.
- New tracking behavior requires sequence tests.
- New UI overlay geometry requires pure geometry tests.
- New performance budget policy requires EditMode tests for budget status and violation semantics.
- New editor setup tools require EditMode tests for scene object creation/update behavior.
- New optional modules require tests that core remains independent from module/vendor dependencies.
- Device/camera behavior should have manual or PlayMode test notes until automated device tests exist.

## Debug Visualization Rules

Production debug visualization must:

- Render from `VisionFrameResult`, not raw model-family output.
- Respect source size, viewport size, aspect ratio, letterbox fit, and clamping.
- Keep labels inside the visible preview.
- Use stable colors from track/class identity.
- Show confidence and track ids when available.
- Render detections, masks, skeleton bones, keypoints, and metrics on separate layers.
- Keep geometry logic testable outside UI Toolkit.
- Avoid per-frame tree growth or unnecessary allocations.

## Git And Assets

- Push meaningful completed increments to `origin/main`.
- Keep `.gitignore` protecting Unity generated folders, IDE files, local model weights, build outputs, logs, and test output.
- Keep large runtime model/media assets under Git LFS via `.gitattributes`.
- Do not commit `Library/`, `Temp/`, `Logs/`, `UserSettings/`, generated `.csproj`, `.sln`, local `.pt` source weights, or local env files.
- Do not revert unrelated user changes.

## Documentation Requirements

When changing architecture, update at least one of:

- `README.md` for user-facing capabilities and setup.
- `TESTING.md` for test counts, commands, or coverage matrix.
- `Packages/com.proanima.universal-vision-tracker/Documentation~/ARCHITECTURE_ROADMAP.md` for architecture increments.
- `Documentation/TECHNOLOGY_RESEARCH.md` for research-level decisions.

Architecture log entries should include:

- what changed;
- why it matters;
- migration impact;
- current test baseline.

## Definition Of Done

A production increment is done only when:

- The code compiles in Unity.
- Relevant EditMode tests pass or the reason they could not run is documented.
- Public architecture direction remains model-agnostic.
- Removed prototype APIs stay removed unless a new adapter contract is explicitly approved.
- Docs reflect new public behavior.
- The commit is pushed when the user asked to keep pushing.

## Near-Term Architecture Priorities

1. Extract `VisionPipeline` orchestration from `UniversalTrackerManager`.
2. Add `VisionPipelineProfile`.
3. Add model profile validation.
4. Expand production `IVisionFrameSource` coverage for camera, texture, video, AR, and XR sources.
5. Add parser/output schema fixtures.
6. Add PlayMode smoke tests for UI Toolkit dashboard.
7. Add screenshot-level PlayMode checks for dashboard layout across common viewport sizes.
8. Add model-profile samples for YOLO detection, pose, and segmentation.
