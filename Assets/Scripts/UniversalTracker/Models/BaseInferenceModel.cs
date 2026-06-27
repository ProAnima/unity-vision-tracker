using UnityEngine;
using Unity.InferenceEngine;
using UnityEngine.Rendering;
using UniversalTracker.Core;
using UniversalTracker.Processing;
using System;
using System.Collections.Generic;

namespace UniversalTracker.Models
{
    /// <summary>
    /// Базовый класс для всех моделей inference
    /// </summary>
    public abstract class BaseInferenceModel : IInferenceModel
    {
        #region Public Properties
        
        public abstract ModelType ModelType { get; }
        public bool IsInitialized { get; protected set; }
        
        #endregion
        
        #region Public Methods
        
        public virtual void Initialize(ModelConfig config)
        {
            if (config?.modelAsset == null)
            {
                Debug.LogError("❌ [BaseModel] ModelAsset null!");
                return;
            }
            
            try
            {
                this.config = config;
                
                model = ModelLoader.Load(config.modelAsset);
                
                if (model == null)
                {
                    Debug.LogError("❌ [BaseModel] Не удалось загрузить модель!");
                    return;
                }
                
                var backend = config.backend;
                if (backend == BackendType.GPUCompute && SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D12)
                {
                    Debug.LogWarning("⚠️ [BaseModel] GPUCompute + DX12 крашит Unity. Переключаюсь на GPUPixel.");
                    backend = BackendType.GPUPixel;
                }
                
                worker = new Worker(model, backend);
                
                if (worker == null)
                {
                    Debug.LogError("❌ [BaseModel] Не удалось создать Worker!");
                    return;
                }
                
                preProcessor = new PreProcessor();
                postProcessor = new PostProcessor();
                nmsProcessor = new NMSProcessor();
                
                IsInitialized = true;
                
                Debug.Log($"✅ [BaseModel] Модель {config.modelName} загружена (Backend: {backend})");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ [BaseModel] Критическая ошибка инициализации: {e.Message}\n{e.StackTrace}");
                
                // Очистка при ошибке
                try
                {
                    worker?.Dispose();
                    inputTensor?.Dispose();
                }
                catch { }
                
                worker = null;
                inputTensor = null;
                model = null;
                IsInitialized = false;
            }
        }
        
        public abstract InferenceResult RunInference(Texture inputTexture);
        
        public virtual void Dispose()
        {
            try
            {
                worker?.Dispose();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ [BaseModel] Ошибка Dispose worker: {e.Message}");
            }
            finally
            {
                worker = null;
            }
            
            try
            {
                inputTensor?.Dispose();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ [BaseModel] Ошибка Dispose tensor: {e.Message}");
            }
            finally
            {
                inputTensor = null;
            }
            
            model = null;
            IsInitialized = false;
            
            Debug.Log($"🔌 [BaseModel] Модель {config?.modelName} выгружена");
        }
        
        #endregion
        
        #region Protected Fields
        
        protected ModelConfig config;
        protected Model model;
        protected Worker worker;
        protected Tensor<float> inputTensor;
        
        protected PreProcessor preProcessor;
        protected PostProcessor postProcessor;
        protected NMSProcessor nmsProcessor;
        
        #endregion
        
        #region Protected Methods
        
        protected Tensor<float> PrepareInput(Texture inputTexture)
        {
            try
            {
                if (inputTexture == null)
                {
                    Debug.LogError("❌ [BaseModel] PrepareInput: входная текстура null!");
                    return null;
                }
                
                // КРИТИЧЕСКАЯ ПРОВЕРКА: текстура должна быть валидной
                if (inputTexture.width <= 0 || inputTexture.height <= 0)
                {
                    Debug.LogError($"❌ [BaseModel] PrepareInput: некорректный размер текстуры {inputTexture.width}x{inputTexture.height}!");
                    return null;
                }
                
                if (config == null)
                {
                    Debug.LogError("❌ [BaseModel] PrepareInput: config null!");
                    return null;
                }
                
                if (inputTensor == null)
                {
                    Debug.Log($"📊 [BaseModel] Создаём тензор [{1}, {3}, {config.inputSize}, {config.inputSize}]...");
                    inputTensor = new Tensor<float>(new TensorShape(1, 3, config.inputSize, config.inputSize));
                    Debug.Log($"✅ [BaseModel] Тензор создан успешно");
                }
                
                Debug.Log($"🔄 [BaseModel] Конвертация текстуры {inputTexture.width}x{inputTexture.height} (тип: {inputTexture.GetType().Name}) → тензор...");
                Debug.Log($"   Texture info: dimension={inputTexture.dimension}, filterMode={inputTexture.filterMode}");
                Debug.Log($"   Тензор: {inputTensor.shape}");
                
                // КРИТИЧЕСКИЙ МОМЕНТ - здесь может быть краш!
                Debug.Log("   >>> Вызов TextureConverter.ToTensor() <<<");
                
                // Используем TextureTransform для правильной конвертации (как в рабочем коде)
                var transform = new TextureTransform().SetTensorLayout(TensorLayout.NCHW);
                TextureConverter.ToTensor(inputTexture, inputTensor, transform);
                
                Debug.Log("   >>> TextureConverter завершился! <<<");
                
                Debug.Log($"✅ [BaseModel] Конвертация успешна");
                
                return inputTensor;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ [BaseModel] КРИТИЧЕСКАЯ ОШИБКА PrepareInput: {e.Message}\n{e.StackTrace}");
                Debug.LogError($"   Текстура: {inputTexture?.width}x{inputTexture?.height}");
                Debug.LogError($"   Тензор: {inputTensor?.shape}");
                
                // Попробуем пересоздать тензор
                try
                {
                    inputTensor?.Dispose();
                    inputTensor = null;
                }
                catch { }
                
                return null;
            }
        }
        
        protected (float[] data, TensorShape shape) ExecuteInferenceWithShape()
        {
            try
            {
                if (worker == null)
                {
                    Debug.LogError("❌ [BaseModel] Worker null!");
                    return (null, default);
                }
                
                if (inputTensor == null)
                {
                    Debug.LogError("❌ [BaseModel] InputTensor null!");
                    return (null, default);
                }
                
                worker.Schedule(inputTensor);
                
                using var outputTensor = worker.PeekOutput() as Tensor<float>;
                
                if (outputTensor == null)
                {
                    Debug.LogError("❌ [BaseModel] OutputTensor null!");
                    return (null, default);
                }
                
                var shape = outputTensor.shape;
                
                using var cpuTensor = outputTensor.ReadbackAndClone() as Tensor<float>;
                
                if (cpuTensor == null)
                {
                    Debug.LogError("❌ [BaseModel] Не удалось получить CPU тензор!");
                    return (null, default);
                }
                
                var buffer = new float[cpuTensor.count];
                cpuTensor.AsReadOnlySpan().CopyTo(buffer);
                
                return (buffer, shape);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ [BaseModel] Ошибка ExecuteInference: {e.Message}\n{e.StackTrace}");
                return (null, default);
            }
        }
        
        protected List<(float[] data, TensorShape shape)> ExecuteInferenceMultiOutput()
        {
            var results = new List<(float[], TensorShape)>();
            
            try
            {
                if (worker == null || inputTensor == null)
                {
                    Debug.LogError("❌ [BaseModel] Worker или InputTensor null!");
                    return results;
                }
                
                worker.Schedule(inputTensor);
                
                // YOLO26-seg обычно имеет 2 выхода: detections и proto_masks
                // Проверим первый выход
                using (var output0 = worker.PeekOutput() as Tensor<float>)
                {
                    if (output0 != null)
                    {
                        using var cpu0 = output0.ReadbackAndClone() as Tensor<float>;
                        if (cpu0 != null)
                        {
                            var buffer0 = new float[cpu0.count];
                            cpu0.AsReadOnlySpan().CopyTo(buffer0);
                            results.Add((buffer0, cpu0.shape));
                        }
                    }
                }
                
                // Проверим второй выход (по имени или индексу)
                try
                {
                    using var output1 = worker.PeekOutput(1) as Tensor<float>;
                    if (output1 != null)
                    {
                        using var cpu1 = output1.ReadbackAndClone() as Tensor<float>;
                        if (cpu1 != null)
                        {
                            var buffer1 = new float[cpu1.count];
                            cpu1.AsReadOnlySpan().CopyTo(buffer1);
                            results.Add((buffer1, cpu1.shape));
                        }
                    }
                }
                catch
                {
                    // Если второго выхода нет - ничего страшного
                }
                
                return results;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ [BaseModel] Ошибка ExecuteInferenceMultiOutput: {e.Message}");
                return results;
            }
        }
        
        protected float[] ExecuteInference()
        {
            var (data, _) = ExecuteInferenceWithShape();
            return data;
        }
        
        #endregion
    }
}
