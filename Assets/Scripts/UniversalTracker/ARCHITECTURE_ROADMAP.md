# Universal Vision Tracker: архитектурный план вывода в production

## 1. Цель продукта

Проект должен стать универсальным инструментом компьютерного зрения для Unity-приложений, а не набором отдельных демо-скриптов под конкретную YOLO-модель.

Целевой продукт:

- трекает людей, скелеты и объекты в реальном времени;
- работает с разными источниками кадров Unity;
- поддерживает несколько семейств моделей через адаптеры;
- отдаёт стабильный единый API для игровых, AR/XR, UI, симуляционных и production-сценариев;
- может быть поставлен как UPM-пакет с runtime, editor tooling, samples и документацией.

Ключевая идея: центром системы должен быть не YOLO и не конкретный backend inference, а единый pipeline `Frame Source -> Preprocess -> Model Runtime -> Postprocess -> Tracking -> Result Bus -> Receivers`.

## 2. Основные сценарии

### 2.1 Human Detection

Система определяет людей в кадре и отдаёт:

- bounding box;
- confidence;
- class label;
- стабильный `trackId`;
- события `appeared`, `updated`, `lost`, `removed`.

Примеры использования:

- интерактивные инсталляции;
- подсчёт людей;
- AR/XR привязка маркеров к людям;
- gameplay triggers;
- safety zones.

### 2.2 Object Detection

Система определяет предметы и категории объектов:

- bbox;
- class id/class label;
- confidence;
- stable object id;
- опционально world-space projection.

Примеры:

- распознавание реквизита;
- ассистенты в AR;
- training/simulation apps;
- QA/inspection workflows;
- gameplay interactions with real-world objects.

### 2.3 Pose / Skeleton Tracking

Система определяет скелет человека:

- набор 2D keypoints;
- confidence по каждой точке;
- связи костей;
- stable `personId`;
- сглаживание;
- фильтрация пропавших точек;
- опциональный bridge к Unity Rig/Animator/IK.

Примеры:

- motion-driven UI;
- gesture recognition;
- character control;
- live animation preview;
- fitness/rehab/training apps;
- virtual production.

### 2.4 Future Scenarios

После стабилизации ядра:

- segmentation;
- instance masks;
- hand tracking;
- face landmarks;
- depth-aware 3D placement;
- multi-camera fusion;
- AR Foundation integration;
- Meta Quest passthrough;
- HoloLens/visionOS-style spatial camera flows;
- server/offline batch processing.

## 3. Архитектурные принципы

### 3.1 Model-agnostic core

Runtime-ядро не должно знать, что такое YOLO11, YOLO26 или BlazePose. Оно должно знать только:

- какая задача решается: detection, pose, segmentation, classification;
- какой вход нужен модели;
- какой parser превращает выходы модели в единый результат.

YOLO должен быть одним из адаптеров, а не фундаментом системы.

### 3.2 Source-agnostic frame pipeline

Источники кадров не должны знать требования конкретной модели. Например, `WebCamFrameSource` не должен жёстко решать, что нужно `640x640`. Он должен отдавать исходный кадр и метаданные, а resize/letterbox/orientation должны выполняться в preprocessing layer.

### 3.3 Unified result model

Все модели должны приводиться к одному публичному API:

- `VisionFrameResult`;
- `VisionDetection`;
- `VisionPose`;
- `VisionKeypoint`;
- `VisionMask`;
- `VisionTrack`;
- `VisionPerformanceStats`.

Приложение не должно менять интеграционный код при переходе с YOLO11 на YOLO12 или другой backend.

### 3.4 Clear production boundaries

Система должна иметь явные границы ответственности:

- input отвечает только за получение кадра;
- preprocess отвечает за подготовку tensor input и coordinate mapping;
- model runtime отвечает за inference;
- parser/postprocess отвечает за интерпретацию выходов;
- tracker отвечает за stable IDs и lifecycle;
- receivers отвечают за отображение и интеграцию.

### 3.5 Unity-first design

Инструмент должен использовать нативные Unity-паттерны:

- `ScriptableObject` profiles;
- inspector-driven setup;
- `MonoBehaviour` components;
- Unity Events;
- `RenderTexture`;
- `Texture`;
- `VideoPlayer`;
- `Camera`;
- AR Foundation;
- XR-specific extensions через отдельные adapters;
- UPM packaging.

## 4. Целевая структура pipeline

```text
Frame Sources
    -> Frame Scheduler
        -> Preprocessor
            -> Model Runtime
                -> Output Parser
                    -> Postprocessor
                        -> Tracking Layer
                            -> Result Bus
                                -> Receivers / Integrations
```

Расширенная схема:

```text
Input Layer
    WebCamFrameSource
    UnityCameraFrameSource
    RenderTextureFrameSource
    TextureFrameSource
    VideoFrameSource
    ARFoundationFrameSource
    XRPassThroughFrameSource
    CustomFrameSource

Processing Layer
    FrameScheduler
    TexturePreprocessor
    TensorAllocator
    CoordinateMapper
    LetterboxTransform

Model Layer
    VisionModelProfile
    IModelRuntime
    InferenceEngineRuntime
    ModelAdapter
    OutputParser

Task Layer
    DetectionTask
    PoseTask
    SegmentationTask
    ClassificationTask

Tracking Layer
    DetectionTracker
    PoseTracker
    TrackLifecycleManager
    Smoothing

Output Layer
    VisionResultBus
    UIOverlayReceiver
    WorldOverlayReceiver
    EventReceiver
    JsonReceiver
    RigMapper
    DebugStatsReceiver
```

## 5. Runtime core

### 5.1 VisionManager

Новый главный компонент. Он должен заменить перегруженный `UniversalTrackerManager` после миграции.

Ответственность:

- хранить активный `VisionPipelineProfile`;
- запускать/останавливать pipeline;
- управлять scheduler;
- публиковать результаты;
- держать health/status;
- управлять active model profiles;
- поддерживать runtime переключение моделей.

Пример публичного API:

```csharp
public sealed class VisionManager : MonoBehaviour
{
    public VisionPipelineProfile pipelineProfile;
    public bool autoStart = true;

    public bool IsRunning { get; }
    public VisionHealthStatus Health { get; }
    public VisionFrameResult LastResult { get; }

    public event Action<VisionFrameResult> FrameProcessed;
    public event Action<VisionError> ErrorReceived;

    public void StartVision();
    public void StopVision();
    public void SwitchModel(VisionModelProfile profile);
}
```

### 5.2 VisionPipelineProfile

`ScriptableObject`, описывающий весь pipeline:

- source settings;
- model profiles;
- tracking settings;
- output/receiver defaults;
- performance profile;
- debug settings.

Пример:

```csharp
[CreateAssetMenu(menuName = "Universal Vision/Pipeline Profile")]
public sealed class VisionPipelineProfile : ScriptableObject
{
    public VisionSourceProfile source;
    public VisionModelProfile[] models;
    public VisionTrackingProfile tracking;
    public VisionPerformanceProfile performance;
    public VisionDebugProfile debug;
}
```

## 6. Unified data model

### 6.1 VisionFrame

Единый кадр от любого источника:

```csharp
public readonly struct VisionFrame
{
    public readonly Texture texture;
    public readonly int frameIndex;
    public readonly double timestamp;
    public readonly Vector2Int sourceSize;
    public readonly VisionFrameOrientation orientation;
    public readonly bool mirroredX;
    public readonly bool mirroredY;
    public readonly Matrix4x4? cameraProjection;
    public readonly Matrix4x4? cameraToWorld;
}
```

### 6.2 VisionFrameResult

Публичный результат одного обработанного кадра:

```csharp
public sealed class VisionFrameResult
{
    public int frameIndex;
    public double timestamp;
    public Vector2Int sourceSize;
    public VisionDetection[] detections;
    public VisionPose[] poses;
    public VisionMask[] masks;
    public VisionClassification[] classifications;
    public VisionPerformanceStats stats;
}
```

### 6.3 VisionDetection

```csharp
public struct VisionDetection
{
    public int trackId;
    public int classId;
    public string label;
    public float confidence;
    public Rect sourceRect;
    public Vector2 sourceCenter;
    public Rect normalizedRect;
    public VisionTrackState trackState;
}
```

### 6.4 VisionPose

```csharp
public struct VisionPose
{
    public int personId;
    public float confidence;
    public Rect sourceRect;
    public VisionKeypoint[] keypoints;
    public VisionSkeletonDefinition skeleton;
    public VisionTrackState trackState;
}
```

### 6.5 VisionKeypoint

```csharp
public struct VisionKeypoint
{
    public int index;
    public string name;
    public Vector2 sourcePosition;
    public Vector2 normalizedPosition;
    public float confidence;
    public bool isVisible;
}
```

## 7. Camera and frame source architecture

Это один из главных отличителей будущего инструмента. Unity-приложения используют камеры по-разному, поэтому источник кадра должен быть расширяемым.

### 7.1 IFrameSource

```csharp
public interface IFrameSource : IDisposable
{
    bool IsReady { get; }
    Vector2Int SourceSize { get; }
    VisionFrameSourceType SourceType { get; }

    void Initialize(VisionSourceProfile profile);
    bool TryGetFrame(out VisionFrame frame);
}
```

### 7.2 WebCamFrameSource

Основан на `WebCamTexture`.

Production-требования:

- выбор камеры по index/name;
- корректная обработка `isFrontFacing`;
- metadata для mirror/orientation;
- ожидание валидного размера камеры;
- опциональный intermediate `RenderTexture`;
- безопасный release;
- не жёстко навязывать размер модели.

Use cases:

- desktop webcam;
- laptop camera;
- USB camera;
- Android/iOS camera через Unity webcam path.

### 7.3 UnityCameraFrameSource

Основан на `Camera.targetTexture`.

Production-требования:

- если у камеры нет `targetTexture`, source создаёт внутренний `RenderTexture`;
- поддержка URP camera stack;
- поддержка отдельной hidden inference camera;
- опциональный downscale;
- возврат `cameraProjection` и `cameraToWorld` для world projection.

Use cases:

- game camera analysis;
- minimap/scene camera inference;
- synthetic data;
- in-game object detection;
- simulation perception.

### 7.4 RenderTextureFrameSource

Принимает внешний `RenderTexture`.

Production-требования:

- не владеть ресурсом, если он внешний;
- проверять created/valid state;
- поддерживать dynamic resolution;
- уметь работать с camera, compute output, compositor output.

Use cases:

- URP/HDRP render target;
- camera feed from another system;
- video compositor;
- streaming texture;
- XR render target.

### 7.5 TextureFrameSource

Для статических или обновляемых `Texture`/`Texture2D`.

Use cases:

- testing;
- model validation;
- image-based demo;
- offline frame stepping.

### 7.6 VideoFrameSource

Основан на `VideoPlayer`.

Production-требования:

- video file/url/clip;
- pause/step frame;
- loop;
- sync timestamp;
- reuse render texture.

Use cases:

- demos;
- QA tests;
- reproducible benchmarks;
- offline analysis.

### 7.7 ARFoundationFrameSource

Отдельный adapter поверх AR Foundation.

Возможные реализации:

- CPU image path через `ARCameraManager.TryAcquireLatestCpuImage`;
- GPU path через camera background texture, если доступен;
- платформенные различия Android/iOS;
- orientation/mirroring;
- camera intrinsics;
- projection to world ray.

Use cases:

- mobile AR object detection;
- AR people detection;
- AR placement relative to detected objects.

### 7.8 XRPassThroughFrameSource

Платформенно-зависимый слой для Quest/других XR устройств.

Требования:

- вынести в optional package/module;
- не делать core зависимым от Meta/Oculus SDK;
- возвращать depth/raycast hooks, если платформа даёт такую возможность;
- работать через compile symbols.

Use cases:

- Quest passthrough detection;
- spatial anchors from detections;
- mixed reality interactions.

### 7.9 CustomFrameSource

Расширяемая точка входа для пользователя.

Examples:

- network camera;
- RTSP stream;
- NDI/Spout/Syphon;
- custom native plugin;
- prerecorded sensor data;
- industrial camera SDK.

## 8. Preprocessing layer

Preprocessing должен стать самостоятельным слоем, общим для всех моделей.

Ответственность:

- resize;
- letterbox;
- crop;
- normalize;
- channel order;
- tensor layout;
- orientation correction;
- mirror correction;
- color space;
- texture-to-tensor conversion;
- reuse tensor buffers.

Ключевая структура:

```csharp
public sealed class PreprocessResult
{
    public Tensor<float> tensor;
    public VisionImageTransform imageTransform;
    public Rect modelInputRect;
    public Vector2Int modelInputSize;
}
```

`VisionImageTransform` должен уметь:

- source -> model coordinates;
- model -> source coordinates;
- normalized -> source coordinates;
- source -> UI overlay coordinates.

Это критично для правильных bbox/keypoints при letterbox, aspect ratio mismatch и mirrored cameras.

## 9. Model runtime layer

### 9.1 IModelRuntime

Абстракция над backend inference.

```csharp
public interface IModelRuntime : IDisposable
{
    bool IsInitialized { get; }
    VisionModelInfo ModelInfo { get; }

    void Initialize(VisionModelProfile profile);
    VisionRawModelOutput Execute(Tensor<float> input);
}
```

Первый runtime:

- `UnityInferenceEngineRuntime`.

Потенциально позже:

- remote inference runtime;
- native plugin runtime;
- mock runtime for tests;
- batch/offline runtime.

### 9.2 VisionModelProfile

Главный asset модели:

```csharp
[CreateAssetMenu(menuName = "Universal Vision/Model Profile")]
public sealed class VisionModelProfile : ScriptableObject
{
    public string modelName;
    public ModelAsset modelAsset;
    public VisionTaskType taskType;
    public VisionModelFamily family;
    public BackendType backend;
    public Vector2Int inputSize;
    public TensorLayout tensorLayout;
    public VisionOutputSchema outputSchema;
    public TextAsset classLabels;
    public float confidenceThreshold;
    public float nmsThreshold;
}
```

Важно: auto-detect по имени файла можно оставить как wizard/helper, но не как единственный источник истины.

### 9.3 Model adapters

```text
Adapters
    YOLO
        YOLOv8DetectionParser
        YOLOv9DetectionParser
        YOLO11DetectionParser
        YOLO12DetectionParser
        YOLO26DetectionParser
        YOLOPoseParser
        YOLOSegmentationParser
    Blaze
        BlazePoseParser
        BlazeFaceParser
    Custom
        CustomOutputParser
```

Каждый adapter должен объявлять:

- поддерживаемые task types;
- expected input layout;
- expected output shapes;
- parser;
- postprocessing requirements.

## 10. Postprocessing layer

Нужно разделить parsing raw tensor output и postprocessing.

### 10.1 Output parser

Parser знает конкретный формат модели.

Пример:

```csharp
public interface IOutputParser
{
    VisionParsedOutput Parse(VisionRawModelOutput raw, VisionModelProfile profile);
}
```

### 10.2 Task postprocessor

Postprocessor приводит parsed output к финальному результату:

- confidence filtering;
- NMS;
- coordinate mapping;
- label resolving;
- skeleton connection resolving;
- mask reconstruction;
- sanity checks.

Важно: NMS не всегда нужен. Некоторые YOLO-модели имеют NMS-free output. Это должно быть частью `VisionOutputSchema`.

## 11. Tracking layer

### 11.1 Detection tracking

Минимальный production набор:

- IOU tracker;
- SORT tracker;
- lifecycle states;
- lost timeout;
- confidence smoothing;
- class-aware association.

Следующий уровень:

- ByteTrack;
- BoT-SORT;
- ReID adapter.

### 11.2 Pose tracking

Pose tracking должен использовать не только bbox, но и похожесть keypoints.

Метрики:

- bbox IoU;
- keypoint distance;
- torso center distance;
- keypoint visibility overlap;
- temporal velocity prediction.

Нужны:

- `personId`;
- сглаживание keypoints;
- interpolation missing keypoints;
- confidence decay;
- skeleton validity score.

### 11.3 Track lifecycle

Единая модель:

```csharp
public enum VisionTrackState
{
    New,
    Tracking,
    Lost,
    Removed
}
```

События:

- `TrackAppeared`;
- `TrackUpdated`;
- `TrackLost`;
- `TrackRemoved`.

## 12. Output and integration layer

Receivers остаются правильной идеей, но должны быть отделены от конкретного формата YOLO.

### 12.1 Runtime receivers

- `VisionEventReceiver`;
- `VisionUIOverlayReceiver`;
- `VisionWorldOverlayReceiver`;
- `VisionDebugReceiver`;
- `VisionJsonReceiver`;
- `VisionRigMapper`;
- `VisionGestureReceiver`.

### 12.2 UI overlay

Должен поддерживать:

- bbox;
- labels;
- confidence;
- track id;
- skeleton bones;
- keypoints;
- masks;
- source aspect ratio;
- mirrored camera;
- RawImage/RectTransform mapping.

### 12.3 World overlay

Должен поддерживать:

- world-space markers;
- projection through Unity Camera;
- raycast placement;
- AR ray placement;
- optional depth provider.

### 12.4 Animation bridge

Отдельный модуль:

- map keypoints to human body landmarks;
- smooth joints;
- drive transforms;
- output events for gestures;
- optional integration with Animation Rigging package.

## 13. Editor tooling

Без editor tools проект будет восприниматься как демо. Для production SDK нужны инструменты.

### 13.1 Setup Wizard

Wizard должен создавать сцену:

- VisionManager;
- выбранный frame source;
- model profile;
- overlay canvas;
- debug panel;
- sample receiver.

### 13.2 Model Profile Creator

Создаёт `VisionModelProfile`:

- выбрать `.onnx`/`.sentis`;
- выбрать task type;
- выбрать model family;
- импортировать labels;
- задать input size/layout;
- выбрать backend;
- сохранить profile asset.

### 13.3 Model Validator

Проверяет:

- model asset assigned;
- expected input shape;
- output count;
- output shapes;
- task compatibility;
- labels count;
- backend compatibility.

### 13.4 Output Shape Inspector

Окно для просмотра выходов модели:

- output names;
- tensor shapes;
- sample min/max;
- first detections preview;
- parser compatibility diagnostics.

### 13.5 Benchmark Runner

Тестирует:

- inference time;
- preprocess time;
- postprocess time;
- allocations;
- FPS;
- backend comparison;
- input size comparison.

## 14. Performance and reliability

### 14.1 Runtime performance

Цели:

- minimum per-frame allocations;
- tensor reuse;
- texture reuse;
- buffer pooling;
- optional frame skipping;
- target FPS limiter;
- separate source FPS and inference FPS;
- adaptive quality profile.

Profiles:

```text
Fast
    lower input size
    lower inference FPS
    lightweight tracker
    minimal visualization

Balanced
    default input size
    moderate FPS
    SORT tracking
    debug stats optional

Accurate
    larger model
    higher confidence processing
    pose smoothing
    advanced tracker
```

### 14.2 Error handling

Нужно ввести `VisionHealthStatus`:

- `NotInitialized`;
- `Initializing`;
- `Running`;
- `Degraded`;
- `Recovering`;
- `Stopped`;
- `Failed`.

Ошибки должны быть структурированы:

```csharp
public sealed class VisionError
{
    public VisionErrorCode code;
    public string message;
    public Exception exception;
    public bool isRecoverable;
}
```

### 14.3 Backend fallback

Текущая защита от `GPUCompute + Direct3D12` должна стать policy:

```text
Backend Policy
    Preferred: GPUCompute / GPUPixel / CPU
    Fallback order: GPUPixel -> CPU
    Platform rules:
        Windows + DX12: avoid known unstable paths if configured
        Mobile: prefer supported GPU/CPU strategy
        Editor: safer debug profile
```

Важно: fallback должен быть явно виден в debug panel и логах.

## 15. Package layout

Целевая структура:

```text
Assets/UniversalVision/
    Runtime/
        Core/
        Sources/
        Processing/
        Models/
        Parsers/
        Tracking/
        Output/
        Utilities/
    Editor/
        SetupWizard/
        ModelProfiles/
        Validators/
        Benchmark/
    Samples/
        01_WebCam_ObjectDetection/
        02_WebCam_HumanDetection/
        03_WebCam_PoseTracking/
        04_UnityCamera_Detection/
        05_Video_OfflineDetection/
        06_ARFoundation_Detection/
    Documentation/
        QuickStart.md
        Architecture.md
        ModelProfiles.md
        CameraSources.md
        Troubleshooting.md
```

После стабилизации:

```text
Packages/com.proanima.universal-vision-tracker/
    package.json
    Runtime/
    Editor/
    Samples~
    Documentation~
```

## 16. Migration from current project

Текущее состояние уже содержит полезные элементы:

- `UniversalTrackerManager`;
- `IInputProvider`;
- `IInferenceModel`;
- `IOutputReceiver`;
- `ITracker`;
- YOLO model classes;
- UI/Event/Debug receivers;
- IOU/SORT tracking;
- WebCamTexture -> RenderTexture safety path.

Миграция должна быть постепенной.

### Phase 1: Stabilize current UniversalTracker

Цель: один надёжный baseline.

Задачи:

- выбрать один эталонный сценарий: webcam -> YOLO detection -> UI overlay;
- синхронизировать `inputSize`, RenderTexture size и documentation;
- убрать противоречия `320 vs 640`;
- определить backend policy;
- убрать жёсткую модельную логику из webcam provider;
- добавить структурированные статусы запуска;
- отделить excessive debug logging от production logging.

### Phase 2: Introduce unified result API

Задачи:

- добавить `VisionFrameResult`;
- добавить `VisionDetection`;
- добавить `VisionPose`;
- добавить `VisionKeypoint`;
- сделать adapter from old `InferenceResult`;
- перевести receivers на новый формат;
- оставить old API как compatibility layer.

### Phase 3: Extract frame sources

Задачи:

- заменить `IInputProvider` на `IFrameSource` или сделать bridge;
- вынести resize/letterbox из input providers;
- сделать `WebCamFrameSource`;
- сделать `RenderTextureFrameSource`;
- сделать `UnityCameraFrameSource`;
- сделать `VideoFrameSource`.

### Phase 4: Model profiles and adapters

Задачи:

- создать `VisionModelProfile`;
- сделать YOLO adapter;
- перенести auto-detect by filename в editor wizard/helper;
- сделать output schema;
- сделать model validator.

### Phase 5: Pose tracking production pass

Задачи:

- унифицировать pose output;
- добавить skeleton definition;
- добавить keypoint smoothing;
- добавить pose tracker;
- добавить rig mapper sample.

### Phase 6: Editor tools and samples

Задачи:

- setup wizard;
- model profile creator;
- benchmark runner;
- debug overlay;
- sample scenes.

### Phase 7: UPM packaging

Задачи:

- перенести в package layout;
- подготовить samples;
- подготовить API docs;
- semantic versioning;
- changelog.

## 17. Minimum viable production scope

Первый production milestone не должен пытаться закрыть всё.

MVP:

- Unity 6 / Inference Engine first;
- WebCam, Unity Camera, RenderTexture, Video sources;
- YOLO detection;
- YOLO pose;
- unified result API;
- UI overlay;
- Event receiver;
- IOU/SORT tracking;
- basic model profiles;
- setup wizard;
- benchmark/debug panel;
- documented fallback backend policy.

Не включать в первый milestone:

- full segmentation production support;
- advanced AR Foundation;
- Quest passthrough;
- ByteTrack;
- multi-camera fusion;
- full Animator retargeting.

Эти пункты должны быть roadmap, а не requirement для первой стабильной версии.

## 18. Definition of done для production SDK

Система считается готовой к первой production-версии, если:

- новый пользователь может создать рабочую сцену через wizard за 2-3 минуты;
- webcam detection работает без ручного редактирования кода;
- pose tracking отдаёт стабильные person IDs и сглаженные keypoints;
- все outputs используют единый `VisionFrameResult`;
- есть минимум 3 sample scenes: object detection, human detection, pose tracking;
- есть validator модели;
- есть benchmark/debug overlay;
- ошибки запуска понятны без чтения исходников;
- backend fallback документирован и виден пользователю;
- public API не зависит от конкретной YOLO-версии;
- camera sources расширяются без изменения core pipeline.

## 19. Главные риски

### 19.1 Output formats

Разные YOLO-версии и экспорты имеют разные output shapes. Нужно не полагаться только на имя файла.

Решение:

- `VisionOutputSchema`;
- validator;
- shape inspector;
- parser diagnostics.

### 19.2 Coordinate mapping

Ошибки в letterbox/resize/mirror/orientation ломают bbox и keypoints.

Решение:

- единый `VisionImageTransform`;
- тесты на mapping;
- visual debug grid;
- image-based validation samples.

### 19.3 Backend instability

GPU paths могут отличаться по платформам.

Решение:

- backend policy;
- fallback;
- benchmark;
- per-platform recommendations.

### 19.4 Scope creep

Система легко разрастётся до всего computer vision сразу.

Решение:

- MVP ограничен detection + pose + базовые sources;
- segmentation/AR/XR как staged roadmap;
- core abstractions проектируются заранее, но реализации добавляются поэтапно.

### 19.5 Технологическая привязка к одному семейству моделей

Если архитектура будет слишком сильно завязана на YOLO, её будет сложно расширить под MediaPipe, SAM-style segmentation, open-vocabulary detection, depth estimation, AR/XR camera feeds и remote/native runtimes.

Решение:

- проектировать API вокруг capabilities, schemas, frame sources и observations;
- держать YOLO как adapter layer;
- развивать `VisionModelProfile`, `IModelRuntime`, `IFrameSource`, `VisionPrompt`, `VisionObservation`;
- сверяться с [TECHNOLOGY_RESEARCH.md](../../../Documentation/TECHNOLOGY_RESEARCH.md) при добавлении крупных подсистем.

## 20. Рекомендуемое следующее действие

Начать с Phase 1:

1. Зафиксировать один эталонный runtime scenario.
2. Привести настройки модели, input size и webcam/render texture path к единой логике.
3. Ввести `VisionFrameResult` рядом со старым `InferenceResult`.
4. Сделать первый bridge: old model output -> new unified result.
5. Перевести UI/Event/Debug receivers на unified result.

После этого можно безопасно разносить систему на новые слои без потери текущей работоспособности.

## 21. Architecture implementation log

### 2026-06-27: Production core increment 1

Первый production-инкремент начинается с безопасного слоя, который не ломает текущий `UniversalTrackerManager`:

- добавлен canonical result API: `VisionFrameResult`, `VisionDetection`, `VisionPose`, `VisionKeypoint`, `VisionMask`, `VisionPerformanceStats`;
- добавлена frame envelope структура `VisionFrame` для будущих camera/frame sources;
- добавлены production enums: task type, source type, frame orientation, track lifecycle, health state, structured error code;
- добавлен тестируемый `VisionImageTransform` для reversible mapping между source image space, model input space и normalized coordinates;
- добавлены EditMode-тесты для result API и координатных преобразований.

Это создаёт базовый контракт, вокруг которого дальше можно мигрировать:

- input providers -> frame sources;
- model outputs -> unified result API;
- UI/Event/Debug receivers -> `VisionFrameResult`;
- postprocessing -> `VisionImageTransform`;
- tracking -> `VisionTrackState`.

### 2026-06-27: Production core increment 2

Второй инкремент вводит compatibility bridge, чтобы старый рабочий inference runtime начал отдавать новый production API без разрыва текущей сцены:

- добавлен `VisionResultAdapter` для конвертации `InferenceResult -> VisionFrameResult`;
- старые normalized `BBoxData.rect` и keypoint positions переводятся в source-space пиксельные координаты;
- `UniversalTrackerManager` публикует `LastVisionResult` и событие `OnVisionFrameResult`;
- `EventOutputReceiver` получил новый UnityEvent `OnVisionFrameReceived`, сохранив legacy events;
- добавлены EditMode-тесты для conversion mapping detection/pose/mask/classification.

Следующий шаг после bridge:

- перевести `DebugOutputReceiver` и UI/debug overlay на `VisionFrameResult`;
- добавить compatibility deprecation notes для legacy `InferenceResult`;
- начать выделение `IFrameSource` поверх текущих `IInputProvider`.

### 2026-06-27: Production testing increment 1

Тестовый слой расширен до core production baseline:

- добавлены unit-тесты для `NMSProcessor`;
- добавлены unit-тесты для `IOUTracker` и `SORTTracker`;
- добавлен GitHub Actions workflow для EditMode-тестов через GameCI;
- тестовая матрица вынесена в `TESTING.md`.

Текущий automated baseline:

```text
EditMode: 25 tests, 25 passed, 0 failed
```

### 2026-06-27: Production core increment 4

Adapter/plugin architecture scaffold added on top of the research direction:

- added capability-based model metadata: `VisionModelCapability`, `VisionModelFamily`, `VisionRuntimeKind`, `VisionModelSourceFormat`;
- added `VisionModelProfile` ScriptableObject with model identity, runtime kind, input/output schema, thresholds, labels, and license metadata;
- added production extension contracts: `IVisionFrameSource`, `IVisionRuntimeAdapter`, `IVisionModelAdapter`, `VisionPipelineContext`;
- added compatibility adapters so current prototype pieces can participate in the new architecture:
  - `LegacyInputProviderFrameSource` wraps existing `IInputProvider`;
  - `LegacyInferenceRuntimeAdapter` wraps existing `IInferenceModel`;
  - `YoloLegacyModelAdapter` keeps YOLO as one adapter family instead of the core runtime identity;
- added EditMode tests for schemas, capabilities, legacy source wrapping, and YOLO profile/config bridging.

Current automated baseline:

```text
EditMode: 35 tests, 35 passed, 0 failed
```

### 2026-06-27: Production core increment 5

`VisionModelProfile` is now part of the active runtime migration path:

- `UniversalTrackerManager` exposes `modelProfiles` and prefers them when configured;
- legacy `ModelConfig[]` remains the fallback path for existing scenes;
- profile-backed startup converts through `YoloLegacyModelAdapter.ToLegacyConfig` while the full graph runtime is still being extracted;
- `SwitchModel` now works against the active model source, either profiles or legacy configs;
- `ActiveModelProfile` exposes the production profile currently driving inference.

This keeps the migration incremental: scenes can move to profile assets now, while YOLO runtime internals continue to work through the compatibility adapter.

### 2026-06-27: Production visualization increment 1

Runtime debug visualization was upgraded around the canonical `VisionFrameResult` overlay path:

- adaptive fit geometry now clamps detections and labels inside the preview viewport;
- stable colors are derived from track/class ids for readable multi-object debugging;
- stroke thickness adapts to viewport size;
- UI Toolkit dashboard has separate layers for masks, bones, detection boxes, keypoints, labels, and metrics;
- mask overlays render contour-style bounds and optional mask textures;
- skeleton rendering respects keypoint visibility/confidence and fades low-confidence bones;
- overlay metrics expose source size, viewport size, fitted content size, and result counts.

Current automated baseline:

```text
EditMode: 39 tests, 39 passed, 0 failed
```

### 2026-06-27: Production pipeline increment 1

The first clean pipeline extraction is in place:

- added `VisionPipeline`, a model-agnostic orchestration core around `IVisionFrameSource -> IVisionRuntimeAdapter -> VisionFrameResult`;
- added `VisionPipelineProfile` as the production ScriptableObject direction for model/runtime/debug/health settings;
- pipeline owns start/stop/process lifecycle, health state, last result, processed-frame events, and structured recoverable/non-recoverable errors;
- pipeline tests cover missing configuration, initialization, successful frame processing, source-not-ready errors, null runtime results, disposal, and profile default-model selection;
- `UniversalTrackerManager` remains a compatibility facade until sources/runtimes are migrated behind the pipeline.

Current automated baseline:

```text
EditMode: 46 tests, 46 passed, 0 failed
```

### 2026-06-27: Production pipeline increment 2

`UniversalTrackerManager` now routes profile-based runtime through `VisionPipeline`:

- added `pipelineProfile` and `useVisionPipeline` manager settings;
- profile-backed startup builds `LegacyInputProviderFrameSource + YoloLegacyModelAdapter + VisionPipeline`;
- legacy `ModelConfig[]` direct inference remains fallback for old scenes;
- `Update()` uses `VisionPipeline.TryProcessNext` when profile pipeline mode is active;
- `EventOutputReceiver` now implements `IVisionFrameResultReceiver` and can receive `VisionFrameResult` directly;
- runtime-created `EventOutputReceiver` instances now initialize UnityEvents safely;
- pipeline results are dispatched through `OnVisionFrameResult` and compatible result receivers.

Current automated baseline:

```text
EditMode: 48 tests, 48 passed, 0 failed
```

### 2026-06-27: Production pipeline increment 3

Profile validation is now a first-class production contract:

- added `VisionProfileValidator`, `VisionProfileValidationReport`, and severity-coded validation messages;
- model profiles validate identity, model family, capabilities, task/capability consistency, runtime asset requirements, input/output schemas, thresholds, license, and provenance metadata;
- pipeline profiles validate model collections, default model index, nested model reports, and duplicate stable IDs;
- `UniversalTrackerManager` blocks profile-backed `VisionPipeline` startup on validation errors while logging warnings for migration metadata gaps;
- EditMode tests cover valid profiles, missing runtime assets, task/capability mismatches, pipeline warnings, duplicate IDs, and report formatting.

Current automated baseline:

```text
EditMode: 53 tests, 53 passed, 0 failed
```
