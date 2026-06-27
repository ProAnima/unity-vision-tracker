<p align="center">
  <img src="Documentation/media/universal-vision-tracker-banner.png" alt="ProAnima Universal Vision Tracker" width="100%">
</p>

<h1 align="center">ProAnima Universal Vision Tracker</h1>

<p align="center">
  Universal computer vision tracking toolkit for Unity: human detection, object detection, pose tracking, and extensible camera sources.
</p>

<p align="center">
  <a href="LICENSE"><img alt="License: MIT" src="https://img.shields.io/badge/License-MIT-green.svg"></a>
  <img alt="Unity" src="https://img.shields.io/badge/Unity-6000.3%2B-black.svg">
  <img alt="Inference Engine" src="https://img.shields.io/badge/Unity%20Inference%20Engine-2.4%2B-blue.svg">
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

- Unity `WebCamTexture` input through a safer `RenderTexture` path.
- Unity `Camera`, `Texture`, and `VideoPlayer` input providers.
- YOLO11 and YOLO26 model wrappers.
- Detection, pose, and segmentation-oriented model classes.
- Unity Inference Engine runtime usage.
- IOU and SORT tracking implementations.
- UI, scene, event, and debug output receivers.
- Runtime manager for model selection, input, inference, tracking, and output.
- Architecture roadmap for a production SDK evolution.

## Target Capabilities

- Unified `VisionFrameResult` API.
- `VisionModelProfile` assets for model configuration.
- Source-agnostic frame pipeline.
- Production-grade preprocess and coordinate mapping.
- Human/object/pose tracking with lifecycle events.
- Skeleton smoothing and rig/animation bridges.
- Editor setup wizard.
- Model validator and output-shape inspector.
- Benchmark/debug overlay.
- UPM package layout: `com.proanima.universal-vision-tracker`.
- Optional AR Foundation and XR passthrough frame sources.

See the full roadmap: [ARCHITECTURE_ROADMAP.md](Assets/Scripts/UniversalTracker/ARCHITECTURE_ROADMAP.md).

## Repository Structure

```text
Assets/
  Models/                         Runtime ONNX models and model config assets
  Scenes/                         Demo scene
  Scripts/UniversalTracker/       Current prototype runtime
  Settings/                       Unity render pipeline settings
Documentation/
  media/                          README and repository visual assets
Packages/                         Unity package manifest
ProjectSettings/                  Unity project settings
```

## Requirements

- Unity `6000.3.4f1` or newer Unity 6 version.
- Unity Inference Engine package.
- Git LFS for model and media assets.

After cloning:

```bash
git lfs install
git lfs pull
```

## Quick Start

1. Clone the repository.
2. Open the project in Unity 6.
3. Open `Assets/Scenes/SampleScene.unity`.
4. Check the `UniversalTrackerManager` object and ensure a model is assigned.
5. Press Play.
6. Watch the Unity Console and UI overlay for camera/inference status.

For current startup troubleshooting, see:

- [STARTUP_CHECKLIST.md](Assets/Scripts/UniversalTracker/STARTUP_CHECKLIST.md)
- [TROUBLESHOOTING.md](Assets/Scripts/UniversalTracker/TROUBLESHOOTING.md)
- [CRASH_FIX.md](Assets/Scripts/UniversalTracker/CRASH_FIX.md)

## Production Roadmap

### Phase 1: Stabilize Current Core

- Lock a reliable webcam -> detection -> UI overlay baseline.
- Resolve `inputSize`, RenderTexture, and backend policy inconsistencies.
- Reduce noisy logs behind debug flags.
- Establish clear health/status reporting.

### Phase 2: Unified Result API

- Introduce `VisionFrameResult`, `VisionDetection`, `VisionPose`, and `VisionKeypoint`.
- Bridge current `InferenceResult` into the new API.
- Move receivers to the unified result format.

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

## Licensing

This project is licensed under the [MIT License](LICENSE).

Forks, modifications, commercial usage, and redistribution are allowed. The MIT License requires retaining the copyright notice and license text in copies or substantial portions of the software.

See also [NOTICE.md](NOTICE.md).

## Credits

Created by **ProAnima**.

This project builds on Unity's modern runtime stack and open model workflows for real-time computer vision inside Unity applications.

