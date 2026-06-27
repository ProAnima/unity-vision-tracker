using UnityEngine;
using Unity.InferenceEngine;

public class TestModelLoad : MonoBehaviour
{
    [Header("Перетащи сюда .onnx модель")]
    public ModelAsset modelAsset;

    [Header("Backend для теста (как в рабочем проекте)")]
    public BackendType backend = BackendType.GPUPixel;
    
    void Start()
    {
        Debug.Log("═══════════════════════════════════");
        Debug.Log("🧪 TEST: Загрузка и выполнение ONNX модели");
        Debug.Log("═══════════════════════════════════");
        
        if (modelAsset == null)
        {
            Debug.LogError("❌ ModelAsset не привязан! Перетащи .onnx файл в Inspector!");
            return;
        }
        
        Model model = null;
        Worker worker = null;
        Tensor<float> inputTensor = null;
        RenderTexture testRT = null;
        
        try
        {
            Debug.Log("▶️ Начало тестов...");
            
            // Тест 1: Загрузка модели
            Debug.Log("📊 Тест 1: Загрузка модели...");
            
            if (modelAsset == null)
            {
                Debug.LogError("❌ modelAsset is NULL!");
                return;
            }
            
            Debug.Log($"   Model asset: {modelAsset.name}");
            Debug.Log("   Вызов ModelLoader.Load()...");
            model = ModelLoader.Load(modelAsset);
            Debug.Log("   ModelLoader.Load() завершился");
            
            if (model == null)
            {
                Debug.LogError("❌ ModelLoader.Load вернул NULL!");
                return;
            }
            
            Debug.Log($"✅ Модель загружена!");
            Debug.Log($"   Inputs: {model.inputs.Count}");
            if (model.inputs.Count > 0)
            {
                Debug.Log($"   Input[0]: {model.inputs[0].name}, shape: {model.inputs[0].shape}");
            }
            Debug.Log($"   Outputs: {model.outputs.Count}");
            if (model.outputs.Count > 0)
            {
                Debug.Log($"   Output[0]: {model.outputs[0].name}");
            }
            
            // Тест 2: Создание Worker с CPU
            Debug.Log($"📊 Тест 2: Создание Worker ({backend})...");
            Debug.Log("   Вызов new Worker()...");
            worker = new Worker(model, backend);
            Debug.Log("   new Worker() завершился");
            
            if (worker == null)
            {
                Debug.LogError("❌ Worker создание вернуло NULL!");
                return;
            }
            
            Debug.Log($"✅ Worker создан ({backend})!");
            
            // Тест 3: Создание входного тензора (640x640 для YOLO!)
            Debug.Log("📊 Тест 3: Создание входного тензора...");
            inputTensor = new Tensor<float>(new TensorShape(1, 3, 640, 640));
            Debug.Log($"✅ Входной тензор создан: {inputTensor.shape}");
            
            // Тест 4: Создание тестовой текстуры и конвертация
            Debug.Log("📊 Тест 4: Конвертация RenderTexture → Tensor...");
            testRT = new RenderTexture(640, 640, 0, RenderTextureFormat.ARGB32);
            testRT.Create();
            
            // Заполняем красным для теста
            RenderTexture.active = testRT;
            GL.Clear(true, true, Color.red);
            RenderTexture.active = null;
            
            Debug.Log("   >>> TextureConverter.ToTensor() с TextureTransform <<<");
            var transform = new TextureTransform().SetTensorLayout(TensorLayout.NCHW);
            TextureConverter.ToTensor(testRT, inputTensor, transform);
            Debug.Log("   >>> Успешно! <<<");
            Debug.Log("✅ Конвертация прошла успешно!");
            
            // Тест 5: Выполнение inference
            Debug.Log("📊 Тест 5: Выполнение inference...");
            
            // Проверки перед Schedule
            if (worker == null)
            {
                Debug.LogError("❌ worker is NULL!");
                return;
            }
            Debug.Log($"   ✓ worker: {worker}");
            
            if (inputTensor == null)
            {
                Debug.LogError("❌ inputTensor is NULL!");
                return;
            }
            Debug.Log($"   ✓ inputTensor: {inputTensor.shape}");
            
            Debug.Log("   ╔═══════════════════════════════════╗");
            Debug.Log("   ║  КРИТИЧЕСКИЙ МОМЕНТ: Schedule    ║");
            Debug.Log("   ╚═══════════════════════════════════╝");
            Debug.Log("   >>> worker.Schedule(inputTensor) <<<");
            
            worker.Schedule(inputTensor);
            
            Debug.Log("   >>> worker.Schedule() УСПЕШНО ЗАВЕРШИЛСЯ! <<<");
            Debug.Log("   ╔═══════════════════════════════════╗");
            Debug.Log("   ║  Schedule прошёл без краша!      ║");
            Debug.Log("   ╚═══════════════════════════════════╝");
            
            Debug.Log("   >>> worker.PeekOutput() <<<");
            var outputTensor = worker.PeekOutput();
            Debug.Log("   >>> PeekOutput() завершился! <<<");
            
            if (outputTensor == null)
            {
                Debug.LogError("❌ Выходной тензор NULL!");
                return;
            }
            
            Debug.Log($"   ✓ Output type: {outputTensor.GetType().Name}");
            Debug.Log($"   ✓ Output shape: {outputTensor.shape}");
            
            using var outputCpuTensor = outputTensor.ReadbackAndClone() as Tensor<float>;
            if (outputCpuTensor == null)
            {
                Debug.LogError("❌ CPU тензор NULL!");
                return;
            }
            
            Debug.Log($"✅ Inference выполнен! Output: {outputCpuTensor.shape}, count: {outputCpuTensor.count}");
            
            Debug.Log("═══════════════════════════════════");
            Debug.Log("✅✅✅ ВСЕ ТЕСТЫ МОДЕЛИ ПРОЙДЕНЫ! ✅✅✅");
            Debug.Log("Модель работает идеально!");
            Debug.Log("═══════════════════════════════════");
        }
        catch (System.Exception e)
        {
            Debug.LogError("═══════════════════════════════════");
            Debug.LogError($"💥 ТЕСТ МОДЕЛИ ПРОВАЛЕН!");
            
            // Безопасный вывод - на случай если e.Message или e.StackTrace null
            try
            {
                Debug.LogError($"Тип ошибки: {(e != null ? e.GetType().Name : "NULL EXCEPTION")}");
                Debug.LogError($"Сообщение: {(e?.Message ?? "NULL MESSAGE")}");
                Debug.LogError($"StackTrace: {(e?.StackTrace ?? "NULL STACKTRACE")}");
            }
            catch
            {
                Debug.LogError("Не удалось вывести детали ошибки!");
            }
            
            Debug.LogError("═══════════════════════════════════");
            Debug.LogError("ПРОБЛЕМА в модели или её выполнении!");
            Debug.LogError("Возможные причины:");
            Debug.LogError("1. Модель повреждена");
            Debug.LogError("2. Неподдерживаемые операторы в модели");
            Debug.LogError("3. Несовместимый формат ONNX");
            Debug.LogError("4. Проблемы с GPU/драйверами");
            Debug.LogError("5. Unity.InferenceEngine не может обработать эту модель");
        }
        finally
        {
            // Очистка
            inputTensor?.Dispose();
            worker?.Dispose();
            if (testRT != null)
            {
                testRT.Release();
                Destroy(testRT);
            }
        }
    }
}
