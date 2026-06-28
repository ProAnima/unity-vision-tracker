<p align="center">
  <img src="Documentation/media/universal-vision-tracker-banner.png" alt="ProAnima Universal Vision Tracker" width="100%">
</p>

<h1 align="center">ProAnima Universal Vision Tracker</h1>

<p align="center">
  Universal computer vision tracking toolkit for Unity: human detection, object detection, pose tracking, and extensible camera sources.
</p>

<p align="center">
  <a href="LICENSE"><img alt="License: MIT" src="https://img.shields.io/badge/License-MIT-green.svg"></a>
  <img alt="Unity" src="https://img.shields.io/badge/Unity-6000.5.1f1-black.svg">
  <img alt="Inference Engine" src="https://img.shields.io/badge/Unity%20Inference%20Engine-2.6.1-blue.svg">
  <img alt="Status" src="https://img.shields.io/badge/status-productionization%20in%20progress-orange.svg">
</p>

---

## What This Is

**ProAnima Universal Vision Tracker** is a Unity-first vision toolkit for real-time detection and tracking workflows.

The project is being shaped into a production-ready SDK layer around Unity's modern inference stack. Its goal is not to be "one more YOLO demo", but a reusable pipeline for Unity applications that need:

- human detection;
- object detection;
- skeleton / pose tracking;
- stable track IDs;
- UI and world-space visualization;
- multiple camera and texture sources;
- model adapters that can evolve beyond one YOLO version.

> Current status: active productionization. The repository contains a working prototype and the architectural roadmap for the production SDK. APIs and folder structure may change before the first stable release.

## Why

Most Unity computer vision examples solve one narrow problem: load one model, run one frame, draw boxes.

Real Unity applications need more:

- camera abstraction;
- robust texture and tensor preprocessing;
- model/runtime separation;
- stable result formats;
- object and pose tracking;
- clean events and receivers;
- debug tooling;
- editor setup workflows;
- platform-specific reliability rules.

This project is designed around that complete pipeline.

## Core Pipeline

```text
Frame Source
  -> Preprocess
    -> Model Runtime
      -> Output Parser
        -> Postprocess
          -> Tracking
            -> Result Bus
              -> Receivers / Integrations
```

The long-term public API is centered around a unified frame result rather than a specific model:

```text
VisionFrameResult
  detections[]
  poses[]
  masks[]
  classifications[]
  stats
```

## Current Capabilities

- Production frame sources for WebCamTexture, Unity Camera, Texture, RenderTexture, and VideoPlayer.
- YOLO-family adapter path through Unity Inference Engine raw tensors and output parsers.
- Unity Inference Engine runtime usage.
- Capability-based `VisionModelProfile` contract for plugin/adapters architecture.
- `VisionPipeline` orchestration for source/runtime/result flow.
- `VisionHealthStatus` lifecycle state and runtime health events.
- Parser registry diagnostics for profile/output compatibility.
- UI Toolkit Control Center as the primary editor entry point for setup, profiles, validation, docs, and samples.
- Profile Compatibility Inspector for runtime, parser, capability, input, and output-schema checks.
- Setup Wizard for creating a profile-driven tracker object in the current scene.
- IOU and SORT tracking implementations.
- Modern UI Toolkit dashboard plus scene, event, UI preview, and debug output receivers.
- Thin runtime manager facade over `VisionPipeline`.
- Embedded UPM package layout: `com.proanima.universal-vision-tracker`.
- Importable Minimal Pipeline, Dashboard Overlay, YOLO Model Profiles, and Experimental Scene samples.
- Optional module package skeletons for AR Foundation, XR passthrough, MediaPipe, native runtimes, and remote runtimes.
- Profile-driven performance/allocation budgets for parser, overlay, tracking, and pipeline stages.
- Dashboard budget, source, model, runtime, health, and last-error status for faster debugging.
- Architecture roadmap for a production SDK evolution.

## Target Capabilities

- Unified `VisionFrameResult` API.
- `VisionModelProfile` assets for model configuration.
- Source-agnostic frame pipeline.
- Production-grade preprocess and coordinate mapping.
- Human/object/pose tracking with lifecycle events.
- Skeleton smoothing and rig/animation bridges.
- Model validator and output-shape inspector.
- Benchmark/debug overlay.
- Optional AR Foundation and XR passthrough frame sources.

See the full roadmap: [ARCHITECTURE_ROADMAP.md](Packages/com.proanima.universal-vision-tracker/Documentation~/ARCHITECTURE_ROADMAP.md).

Start here for setup: [GETTING_STARTED.md](Packages/com.proanima.universal-vision-tracker/Documentation~/GETTING_STARTED.md).

Technology research and future-proofing notes: [TECHNOLOGY_RESEARCH.md](Documentation/TECHNOLOGY_RESEARCH.md).

## Repository Structure

```text
Assets/
  Models/                         Runtime ONNX models and model config assets
  Scenes/                         Demo scene
  Settings/                       Unity render pipeline settings
Documentation/
  media/                          README and repository visual assets
Packages/
  com.proanima.universal-vision-tracker/
    Runtime/                       Production runtime package code
    Editor/                        Editor tooling
    Documentation~/                Package architecture docs
    Samples~/                      Importable package samples
  manifest.json                    Unity project package manifest
ProjectSettings/                  Unity project settings
```

## Requirements

- Unity `6000.5.1f1` for current validation.
- Unity `6000.0+` compatibility target for the package core.
- Unity Inference Engine `2.6.1`.
- Git LFS for model and media assets.

After cloning:

```bash
git lfs install
git lfs pull
```

## Quick Start

1. Clone the repository.
2. Open the project in Unity 6.
3. Open `Tools/ProAnima Vision/Control Center`.
4. Open the Experimental Scene or create profiles from the Control Center.
5. Use Setup Wizard to create or update a tracker object.
6. Press Play and watch the UI Toolkit dashboard.

For the current profile-first setup path, use [GETTING_STARTED.md](Packages/com.proanima.universal-vision-tracker/Documentation~/GETTING_STARTED.md).

For the fastest visual smoke test, import the `Experimental Scene` sample and open `ProAnimaVisionExperimentalScene.unity`.

For setup rules and contribution constraints, see [CODEX.md](CODEX.md).

## Runtime Dashboard

The production UI path is `VisionToolkitDashboardReceiver`, a UI Toolkit receiver driven by the unified `VisionFrameResult` API.

It provides:

- live camera/texture preview;
- adaptive detection boxes with clamped labels, confidence, and stable track colors;
- pose keypoints and skeleton bones with confidence-aware visibility;
- mask overlays with contour-style bounds;
- source/view/fit metrics for diagnosing aspect-ratio and letterbox issues;
- frame, FPS, inference, budget, source, model, runtime, detection, pose, and error counters;
- last error details from `VisionHealthStatus`;
- runtime Start/Stop controls bound to `UniversalTrackerManager`.

Usage:

1. Add a `UIDocument` and `VisionToolkitDashboardReceiver` to a scene object.
2. Assign the `UniversalTrackerManager` reference, or leave `autoFindManager` enabled.
3. Assign the receiver to `UniversalTrackerManager.manualToolkitDashboardReceiver`, or enable `useToolkitDashboard` for dynamic creation.

`UIVisualizationReceiver` remains available as a lightweight Canvas/RawImage preview receiver.

## Plugin And Adapter Architecture

The production runtime is moving toward a model-agnostic adapter layer:

```text
IVisionFrameSource
  -> IVisionRuntimeAdapter
    -> VisionFrameResult
```

Current bridge contracts:

- `VisionModelProfile` describes task, model family, capabilities, runtime kind, input/output schema, thresholds, and license metadata.
- `IVisionFrameSource` is the source-agnostic frame API for webcams, Unity cameras, render textures, videos, AR/XR feeds, and custom cameras.
- Built-in production frame sources cover Texture, RenderTexture, Unity Camera, WebCamTexture, and VideoPlayer paths.
- `IVisionRuntimeAdapter` is the runtime plugin surface for Unity Inference Engine, MediaPipe, native plugins, remote inference, and mocks.
- `VisionAdapterRegistry` resolves model profiles to runtime adapters without hardcoding one model family in the manager.
- `IVisionOutputParser` converts raw runtime tensors into canonical detections, poses, masks, and classifications.
- `UnityInferenceRuntimeAdapter` runs the raw-output parser path into canonical `VisionFrameResult` objects.
- `UnityInferenceRawOutputProvider` executes Unity Inference models into `VisionRawModelOutput` tensors using profile-declared output schemas.
- `VisionProfileValidator` checks profile identity, task/capability consistency, runtime asset requirements, schemas, thresholds, and model governance metadata.
- `YoloModelAdapter` and `UnityInferenceRuntimeAdapter` are the production Unity Inference bridge.
- `UniversalTrackerManager` runs profile-based setups through `VisionPipeline`.

The design keeps YOLO as one adapter instead of the core identity of the SDK.

## Production Roadmap

### Phase 1: Stabilize Current Core

- Lock a reliable webcam -> detection -> UI overlay baseline.
- Resolve `inputSize`, RenderTexture, and backend policy inconsistencies.
- Reduce noisy logs behind debug flags.
- Establish clear health/status reporting.

### Phase 2: Unified Result API

- Introduce `VisionFrameResult`, `VisionDetection`, `VisionPose`, and `VisionKeypoint`.
- Keep receivers on the unified result format.

### Phase 3: Camera Sources

- Extract source-agnostic `IFrameSource`.
- Add production-ready sources for WebCam, Unity Camera, RenderTexture, Texture, and Video.
- Keep AR Foundation and XR passthrough as optional modules.

### Phase 4: Model Profiles

- Introduce `VisionModelProfile`.
- Move model selection away from filename-only detection.
- Add validator and output-shape inspector.

### Phase 5: Pose Tracking

- Stabilize skeleton output.
- Add keypoint smoothing.
- Add pose-specific tracking.
- Build rig/animation sample.

### Phase 6: SDK Packaging

- Move to UPM package layout.
- Add setup wizard.
- Add samples and API docs.
- Prepare first stable release.

## Testing

EditMode tests are available for the production-core API and coordinate mapping layer.

Current baseline:

```text
EditMode: 123 tests, 123 passed, 0 failed
PlayMode: 2 tests, 2 passed, 0 failed
```

See [TESTING.md](TESTING.md) for the batchmode command and result-file notes.

## Licensing

This project is licensed under the [MIT License](LICENSE).

Forks, modifications, commercial usage, and redistribution are allowed. The MIT License requires retaining the copyright notice and license text in copies or substantial portions of the software.

See also [NOTICE.md](NOTICE.md).

## Credits

Created by **ProAnima**.

This project builds on Unity's modern runtime stack and open model workflows for real-time computer vision inside Unity applications.
