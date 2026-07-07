# Technology Research: Future-Proof Unity Vision Runtime

This document captures current technology directions and architectural bets for turning ProAnima Universal Vision Tracker into a long-lived, extensible Unity vision runtime.

The key product decision:

> Do not build a YOLO package. Build a model-agnostic Unity perception runtime.

YOLO should be one adapter. MediaPipe, SAM-style segmentation, depth models, AR/XR camera feeds, remote inference, and future models should fit into the same core pipeline.

## 1. Unity Inference Engine as the Primary Runtime

Unity's modern ML stack is now centered around `com.unity.ai.inference` / `Unity.InferenceEngine`. This should be the first-class runtime backend.

References:

- [Unity Inference Engine package docs](https://docs.unity3d.com/Packages/com.unity.ai.inference@latest/)
- [Unity discussion: using Unity AI Inference in Unity 6.2 beta](https://discussions.unity.com/t/using-unity-ai-inference-in-unity-6-2-beta/1662757)

Architectural implication:

```text
IModelRuntime
    UnityInferenceEngineRuntime
    MediaPipeRuntime
    NativePluginRuntime
    RemoteRuntime
    MockRuntime
```

The production core should depend on `IModelRuntime`, not directly on YOLO or any one model family.

## 2. Model Format Strategy: ONNX, Imported ModelAsset, and Sentis/Inference Assets

Unity workflows often start from ONNX, but production runtime should be based on imported/validated model assets and explicit profiles.

Reference:

- [Unity discussion: loading ONNX models programmatically at runtime](https://discussions.unity.com/t/loading-onnx-models-programmatically-at-runtime-from-scripts/1685292)

Architectural implication:

`VisionModelProfile` should describe both the source model and runtime model:

```text
VisionModelProfile
    modelAsset
    sourceModelFormat: ONNX / Sentis / Native / Remote
    runtimeFormat: ModelAsset / External / Remote
    modelFamily
    taskType
    capabilities
    inputSchema
    outputSchema
    parserId
    labels
    modelLicense
    benchmarkProfile
```

Filename detection can exist as a wizard convenience, but it should not be the core source of truth.

## 3. Camera and Frame Sources: GPU-First, CPU-When-Needed

Unity applications need many frame sources:

- `WebCamTexture`
- `Camera.targetTexture`
- external `RenderTexture`
- static/dynamic `Texture`
- `VideoPlayer`
- AR Foundation camera image
- XR passthrough camera
- custom native/network camera feeds

AR Foundation CPU image acquisition is useful but expensive because it can require GPU-to-CPU transfer.

Reference:

- [AR Foundation CPU camera image docs](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@4.2/manual/cpu-camera-image.html)

## 4. Pose Retargeting: Model Output To Humanoid Intent

Pose models should not drive Unity rigs directly. A parser should emit `VisionPose`, then a retargeting layer should convert model-family keypoints into a canonical humanoid pose that rig receivers can consume.

Current increment:

- `VisionCocoHumanoidPoseRetargeter` maps COCO-17 keypoints into `VisionHumanoidPose`.
- `VisionPoseTemporalFilter` holds and predicts short keypoint dropouts before confidence decays.
- `YOLO Humanoid Retargeting` sample builds a generated humanoid test rig and shows COCO pose adaptation without an external FBX.

Why it matters:

- YOLO, MediaPipe, Kinect-style skeletons, and future body-pose models can converge on the same humanoid receiver contract.
- Runtime rig code can target Animator bones, generic transforms, or IK targets without depending on a specific detector.
- Keypoint instability is handled before rig application, so short wrist/ankle losses do not snap limbs to bind pose.

Migration impact:

- Existing `VisionPose` outputs remain unchanged.
- Rig integrations should consume `VisionHumanoidPose` as an optional downstream receiver rather than bypassing `VisionFrameResult`.

Architectural implication:

```text
IFrameSource
    WebCamFrameSource
    UnityCameraFrameSource
    RenderTextureFrameSource
    TextureFrameSource
    VideoFrameSource
    ARFoundationFrameSource
    XRPassThroughFrameSource
    CustomFrameSource
```

Each frame should carry metadata:

```text
VisionFrame
    texture
    frameIndex
    timestamp
    sourceSize
    sourceType
    orientation
    mirroredX / mirroredY
    camera intrinsics
    projection matrix
    cameraToWorld
    ownership/release policy
```

Input sources must not hardcode model input sizes. Resize, letterbox, and orientation correction belong in preprocessing.

## 4. XR and Spatial Vision

Meta's Quest camera/passthrough samples show a strong direction: on-device camera feed -> Unity Inference Engine -> detections -> depth raycast/spatial anchors.

References:

- [Meta Multi Object Detection sample](https://developers.meta.com/horizon/documentation/unity/unity-sample-camera-object-detection/)
- [Meta Inference Engine for on-device ML/CV](https://developers.meta.com/horizon/documentation/unity/unity-pca-sentis/)
- [Unity-PassthroughCameraApiSamples](https://github.com/oculus-samples/Unity-PassthroughCameraApiSamples)

Architectural implication:

Add a spatial resolver layer instead of mixing world placement into detection parsers:

```text
IVisionSpatialResolver
    ScreenRaycastResolver
    PhysicsRaycastResolver
    ARDepthResolver
    QuestDepthRaycastResolver
    CustomDepthResolver
```

Output:

```text
VisionWorldObject
    detection
    worldPosition
    worldRotation
    anchorId
    confidence
    depthSource
```

This keeps 2D detection, depth, and world placement composable.

## 5. Open-Vocabulary Detection

Open-vocabulary detection allows users to search for classes by text prompts instead of being locked to a fixed COCO class list.

References:

- [YOLO-World GitHub](https://github.com/ailab-cvc/yolo-world)
- [Ultralytics YOLO-World docs](https://docs.ultralytics.com/models/yolo-world/)

Architectural implication:

Model profiles should support dynamic class prompts:

```text
VisionModelCapability.OpenVocabulary
VisionPromptSet
ClassEmbeddingProvider
TextPromptProvider
```

The public API should not assume that labels are always static.

## 6. Promptable Segmentation and SAM-Style Workflows

SAM 2 introduces a different workflow: promptable segmentation, video memory, and interactive object tracking.

Reference:

- [Meta SAM 2](https://ai.meta.com/research/sam2/)

Architectural implication:

Add prompt abstractions early, even if SAM-style runtime is not in MVP:

```text
VisionPrompt
    PointPrompt
    BoxPrompt
    TextPrompt
    MaskPrompt
    TrackPrompt
```

Future usage:

```csharp
vision.Segment(frame, new BoxPrompt(detection.sourceRect));
vision.TrackMask(maskId);
```

## 7. Depth as a First-Class Modality

Monocular depth models are becoming practical enough to support approximate spatial reasoning without a depth sensor.

Reference:

- [Depth Anything V2](https://github.com/DepthAnything/Depth-Anything-V2)

Architectural implication:

Depth should not be embedded inside detection. Treat it as an independent modality:

```text
VisionDepthMap
    texture
    sourceSize
    depthKind: Relative / Metric
    confidence
    transform
```

Compositions:

```text
detection + depthMap -> approximate world object
pose + depthMap -> pseudo-3D skeleton
mask + depthMap -> object volume estimate
```

## 8. Tracking: Observation-Agnostic Instead of BBox-Only

Modern multi-object tracking should not be limited to bounding boxes. ByteTrack is strong for object detection because it uses low-confidence detections to preserve tracks. BoT-SORT adds stronger association ideas. Norfair is useful architecturally because it tracks arbitrary points.

References:

- [ByteTrack](https://github.com/FoundationVision/ByteTrack)
- [BoT-SORT](https://github.com/NirAharon/BoT-SORT)
- [Norfair](https://github.com/tryolabs/norfair)

Architectural implication:

Design tracking around observations:

```text
VisionObservation
    BBoxObservation
    KeypointObservation
    MaskObservation
    WorldPointObservation
    EmbeddingObservation
```

Then tracking can support:

- objects by bbox;
- people by pose keypoints;
- hands by landmarks;
- masks by mask overlap;
- AR objects by world position;
- appearance/ReID embeddings later.

## 9. MediaPipe as an Optional Runtime

MediaPipe remains strong for hands, face, and pose landmarks. The Unity ecosystem already has serious integration work around it.

Reference:

- [MediaPipeUnityPlugin](https://github.com/homuler/MediaPipeUnityPlugin)

Architectural implication:

Do not force all landmark workflows through Unity Inference Engine. Add optional modules:

```text
com.proanima.universal-vision-tracker.mediapipe
```

But normalize outputs into the same public result layer:

```text
VisionPose
VisionHand
VisionFace
VisionLandmarkSet
```

## 10. Capability-Based API

The runtime should ask what a model can do, not what model family it belongs to.

Recommended flags:

```csharp
[Flags]
public enum VisionModelCapability
{
    Detection = 1 << 0,
    HumanDetection = 1 << 1,
    Pose2D = 1 << 2,
    Pose3D = 1 << 3,
    Segmentation = 1 << 4,
    Classification = 1 << 5,
    Depth = 1 << 6,
    OpenVocabulary = 1 << 7,
    Promptable = 1 << 8,
    TemporalMemory = 1 << 9,
    WorldProjection = 1 << 10
}
```

This should drive editor validation, runtime routing, and feature availability.

## 11. Pipeline Graph Direction

The current `UniversalTrackerManager` should gradually become orchestration around a graph/pipeline:

```text
VisionPipeline
    SourceNode
    PreprocessNode
    RuntimeNode
    ParserNode
    TrackingNode
    SpatialResolveNode
    OutputNode
```

This enables:

- multi-model pipelines;
- detection -> pose crop;
- detection -> segmentation prompt;
- detection + depth -> world object;
- different source FPS and inference FPS;
- replay/testing pipelines;
- runtime graph presets.

## 12. Replay-First Testing

Production confidence needs more than unit tests. The next layers should support deterministic replay:

```text
TestAssets/
    images/
    videos/
    model-output-fixtures/
    expected-results/
```

Test categories:

- coordinate mapping tests;
- parser golden-output tests;
- tracker sequence tests;
- replay video tests;
- no-allocation/performance smoke tests;
- model profile validation tests.

## 13. Model/Profile Validator

A production Unity vision SDK needs editor diagnostics.

Validator checks:

- model asset assigned;
- input shape;
- output count;
- output shapes;
- parser compatibility;
- label count;
- backend support;
- model license;
- estimated memory;
- sample inference.

This should become one of the central user-facing editor tools.

## 14. BodyPixSentis Lessons: GPU-First Debug Outputs

Reviewed reference:

- [keijiro/BodyPixSentis](https://github.com/keijiro/BodyPixSentis), commit `6fb4954439f48d11660e7097d74688f4fbc3fec4`.

BodyPixSentis is valuable as a performance reference because it keeps the primary output path on the GPU:

- inference uses `BackendType.GPUCompute`;
- model input uses an explicit `TextureTransform` with NHWC layout and top-left coordinate origin;
- segmentation output is rendered directly from a tensor into a `RenderTexture`;
- keypoint postprocess runs in a compute shader and writes a `GraphicsBuffer`;
- CPU keypoint readback exists as a convenience cache, not as the required render path;
- mask visualization is shader-based with smooth thresholding instead of CPU mask polygon reconstruction;
- model variants expose clear quality/performance knobs through width multiplier and stride.

Production implication for this project:

```text
canonical CPU-safe results:
    VisionDetection / VisionPose / VisionMask / VisionClassification

optional GPU debug outputs:
    RenderTexture maskTexture
    GraphicsBuffer keypointBuffer
    future depth/embedding buffers
```

The canonical `VisionFrameResult` remains the public integration contract, but adapters may attach GPU output handles for high-performance overlays. UI/debug receivers should prefer GPU handles when available, and fall back to CPU/normalized geometry when not.

Do not copy BodyPixSentis implementation details directly into the core package. Instead, use the pattern:

- fused model/profile metadata for preprocessing and postprocessing;
- optional GPU output handles in result objects;
- shader/compute visualization modules behind adapters;
- CPU readback only for API consumers, tests, logging, or export.

Near-term code direction:

1. Add GPU output handles to `VisionFrameResult`.
2. Add shader-based mask overlay path for models that produce mask `RenderTexture`.
3. Add optional keypoint `GraphicsBuffer` rendering path for pose/landmark adapters.
4. Add model-profile fields for output stride/resolution and GPU-output policy.
5. Keep UI Toolkit overlay behavior equivalent when GPU handles are absent.

## Recommended Next Code Increments

1. Add `VisionModelCapability`.
2. Add `VisionModelProfile` ScriptableObject skeleton.
3. Add `VisionInputSchema` and `VisionOutputSchema`.
4. Keep frame ingestion on `IVisionFrameSource` and add optional camera modules behind adapters.
5. Add `VisionPipelineContext`.
6. Add validation tests that do not require a real model asset.
