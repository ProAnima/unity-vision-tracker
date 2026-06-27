# 🔧 Unity InferenceEngine API Reference

## ✅ Правильный Namespace

```csharp
using Unity.InferenceEngine;  // ✅ ПРАВИЛЬНО (новый API)
// НЕ using Unity.Sentis;     // ❌ Устарел
```

Unity переименовал Sentis → InferenceEngine в последних версиях.

---

## 📦 Основные классы

### ModelAsset
```csharp
// Загрузка модели из Assets
Unity.InferenceEngine.ModelAsset modelAsset;
Model model = ModelLoader.Load(modelAsset);
```

### Worker
```csharp
// Создание worker для inference
BackendType backend = BackendType.GPUCompute; // или GPUPixel, CPU
Worker worker = new Worker(model, backend);
```

### Tensor<float>
```csharp
// Создание тензора
var shape = new TensorShape(1, 3, 640, 640); // [batch, channels, height, width]
var tensor = new Tensor<float>(shape);
```

### TextureConverter
```csharp
// Конвертация текстуры в тензор
Texture inputTexture = ...;
Tensor<float> tensor = new Tensor<float>(new TensorShape(1, 3, 640, 640));

TextureConverter.ToTensor(inputTexture, tensor);
```

---

## 🎯 Inference Pipeline

```csharp
// 1. Подготовка входа
Texture inputTexture = camera.texture;
Tensor<float> inputTensor = new Tensor<float>(new TensorShape(1, 3, 640, 640));
TextureConverter.ToTensor(inputTexture, inputTensor);

// 2. Запуск inference
worker.Schedule(inputTensor);

// 3. Получение результата
using var outputTensor = worker.PeekOutput() as Tensor<float>;
using var cpuTensor = outputTensor.ReadbackAndClone() as Tensor<float>;

// 4. Чтение данных
float[] outputData = new float[cpuTensor.count];
cpuTensor.AsReadOnlySpan().CopyTo(outputData);

// 5. Очистка
inputTensor.Dispose();
```

---

## 🔄 BackendType

```csharp
public enum BackendType
{
    GPUCompute,  // Compute Shaders (рекомендуется для GPU)
    GPUPixel,    // Pixel Shaders
    CPU          // CPU inference
}
```

### Выбор backend:
- **GPUCompute**: Лучшая производительность на GPU
- **CPU**: Для устройств без GPU или отладки
- **GPUPixel**: Legacy, обычно медленнее GPUCompute

---

## 📊 TensorShape

```csharp
// NCHW формат (стандарт для YOLO)
var shape = new TensorShape(
    batch: 1,
    channels: 3,     // RGB
    height: 640,
    width: 640
);

// Доступ к размерам
int batch = shape[0];
int channels = shape[1];
int height = shape[2];
int width = shape[3];
```

---

## 🧹 Memory Management

### Важно! Всегда очищайте ресурсы:

```csharp
// Worker
worker?.Dispose();

// Tensor
inputTensor?.Dispose();
outputTensor?.Dispose();

// Или используйте using
using var tensor = new Tensor<float>(shape);
using var output = worker.PeekOutput() as Tensor<float>;
```

---

## 🚀 Оптимизация

### 1. Переиспользуйте тензоры
```csharp
// ❌ Плохо - создается каждый кадр
void Update() {
    var tensor = new Tensor<float>(shape); // аллокация!
    // ...
    tensor.Dispose();
}

// ✅ Хорошо - создается один раз
Tensor<float> tensor;
void Start() {
    tensor = new Tensor<float>(shape);
}
void Update() {
    TextureConverter.ToTensor(texture, tensor); // переиспользование
}
```

### 2. Асинхронный readback (продвинуто)
```csharp
// Используйте ReadbackAndClone() для асинхронности
using var cpuTensor = outputTensor.ReadbackAndClone();
// Обработка в фоне возможна
```

### 3. Batch inference
```csharp
// Обрабатывайте несколько изображений за раз
var shape = new TensorShape(4, 3, 640, 640); // batch=4
```

---

## ⚠️ Частые ошибки

### 1. Забыли Dispose
```csharp
// ❌ Memory leak!
var tensor = new Tensor<float>(shape);
// ... забыли вызвать Dispose()

// ✅ Правильно
var tensor = new Tensor<float>(shape);
try {
    // работа
} finally {
    tensor?.Dispose();
}

// Или
using var tensor = new Tensor<float>(shape);
```

### 2. Неправильный формат тензора
```csharp
// ❌ NHWC вместо NCHW
var shape = new TensorShape(1, 640, 640, 3); // неправильно для YOLO!

// ✅ NCHW
var shape = new TensorShape(1, 3, 640, 640); // правильно
```

### 3. Чтение с GPU без readback
```csharp
// ❌ PeekOutput() возвращает GPU тензор!
var output = worker.PeekOutput();
// Нельзя читать напрямую

// ✅ Нужен readback на CPU
var gpuOutput = worker.PeekOutput() as Tensor<float>;
using var cpuOutput = gpuOutput.ReadbackAndClone() as Tensor<float>;
float[] data = new float[cpuOutput.count];
cpuOutput.AsReadOnlySpan().CopyTo(data);
```

---

## 📝 Полный пример

```csharp
using UnityEngine;
using Unity.InferenceEngine;

public class YOLOInference : MonoBehaviour
{
    public ModelAsset modelAsset;
    public Texture2D inputTexture;
    
    private Model model;
    private Worker worker;
    private Tensor<float> inputTensor;
    
    void Start()
    {
        // Загрузка модели
        model = ModelLoader.Load(modelAsset);
        
        // Создание worker
        worker = new Worker(model, BackendType.GPUCompute);
        
        // Создание входного тензора
        inputTensor = new Tensor<float>(new TensorShape(1, 3, 640, 640));
        
        Debug.Log("✅ Модель загружена");
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RunInference();
        }
    }
    
    void RunInference()
    {
        // 1. Конвертация текстуры
        TextureConverter.ToTensor(inputTexture, inputTensor);
        
        // 2. Inference
        worker.Schedule(inputTensor);
        
        // 3. Получение результата
        using var outputTensor = worker.PeekOutput() as Tensor<float>;
        using var cpuTensor = outputTensor.ReadbackAndClone() as Tensor<float>;
        
        // 4. Обработка
        var outputData = new float[cpuTensor.count];
        cpuTensor.AsReadOnlySpan().CopyTo(outputData);
        
        Debug.Log($"Output size: {outputData.Length}");
        Debug.Log($"Output shape: {cpuTensor.shape}");
    }
    
    void OnDestroy()
    {
        // Очистка
        worker?.Dispose();
        inputTensor?.Dispose();
    }
}
```

---

## 🔗 Полезные ссылки

- [Unity InferenceEngine Docs](https://docs.unity3d.com/Packages/com.unity.inferenceengine@latest/)
- [Sentis → InferenceEngine Migration](https://docs.unity3d.com/Manual/inference-engine-migration.html)

---

**Актуально для Unity InferenceEngine (бывший Sentis)** 🎯
