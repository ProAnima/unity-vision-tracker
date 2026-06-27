using UnityEngine;
using Unity.InferenceEngine;

public class TestSimpleInference : MonoBehaviour
{
    void Start()
    {
        Debug.Log("═══════════════════════════════════");
        Debug.Log("🧪 TEST: Проверка Unity.InferenceEngine");
        Debug.Log("═══════════════════════════════════");
        
        try
        {
            Debug.Log("✓ Unity.InferenceEngine namespace доступен");
            
            // Тест 1: Создание простого тензора
            Debug.Log("📊 Тест 1: Создание Tensor...");
            var tensor = new Tensor<float>(new TensorShape(1, 3, 64, 64));
            Debug.Log($"✅ Tensor создан: {tensor.shape}");
            tensor.Dispose();
            
            // Тест 2: Создание RenderTexture
            Debug.Log("📊 Тест 2: Создание RenderTexture...");
            var rt = new RenderTexture(320, 240, 0, RenderTextureFormat.ARGB32);
            rt.Create();
            Debug.Log($"✅ RenderTexture создан: {rt.width}x{rt.height}");
            
            // Тест 3: TextureConverter
            Debug.Log("📊 Тест 3: TextureConverter.ToTensor...");
            var inputTensor = new Tensor<float>(new TensorShape(1, 3, 64, 64));
            
            // Создаём временную маленькую текстуру для теста
            var testRT = new RenderTexture(64, 64, 0, RenderTextureFormat.ARGB32);
            testRT.Create();
            RenderTexture.active = testRT;
            GL.Clear(true, true, Color.red);
            RenderTexture.active = null;
            
            Debug.Log("   >>> Вызов TextureConverter.ToTensor() <<<");
            TextureConverter.ToTensor(testRT, inputTensor);
            Debug.Log("   >>> TextureConverter завершился! <<<");
            Debug.Log("✅ TextureConverter работает!");
            
            inputTensor.Dispose();
            testRT.Release();
            rt.Release();
            
            Debug.Log("═══════════════════════════════════");
            Debug.Log("✅ ВСЕ ТЕСТЫ ПРОЙДЕНЫ!");
            Debug.Log("Проблема НЕ в Unity.InferenceEngine!");
            Debug.Log("═══════════════════════════════════");
        }
        catch (System.Exception e)
        {
            Debug.LogError("═══════════════════════════════════");
            Debug.LogError($"💥 ТЕСТ ПРОВАЛЕН: {e.Message}");
            Debug.LogError($"StackTrace: {e.StackTrace}");
            Debug.LogError("═══════════════════════════════════");
            Debug.LogError("ПРОБЛЕМА НАЙДЕНА! Unity.InferenceEngine не работает!");
            Debug.LogError("Возможные причины:");
            Debug.LogError("1. Пакет не установлен");
            Debug.LogError("2. Несовместимая версия Unity");
            Debug.LogError("3. Отсутствуют нативные библиотеки");
        }
    }
}
