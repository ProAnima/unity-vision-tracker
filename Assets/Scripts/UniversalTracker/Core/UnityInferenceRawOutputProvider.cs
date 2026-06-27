using System;
using Unity.InferenceEngine;
using UnityEngine;
using UnityEngine.Rendering;

namespace UniversalTracker.Core
{
    public sealed class UnityInferenceRawOutputProvider : IVisionRawOutputProvider
    {
        private readonly VisionModelProfile profile;
        private Model model;
        private Worker worker;
        private Tensor<float> inputTensor;

        public UnityInferenceRawOutputProvider(VisionModelProfile profile)
        {
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
        }

        public bool IsInitialized { get; private set; }

        public void Initialize(VisionModelProfile runtimeProfile)
        {
            if (profile.modelAsset == null)
                throw new InvalidOperationException("ModelAsset is required for Unity inference raw output provider.");

            model = ModelLoader.Load(profile.modelAsset);
            if (model == null)
                throw new InvalidOperationException("ModelLoader returned null.");

            worker = new Worker(model, ResolveBackend(profile.backend));
            inputTensor = CreateInputTensor(profile.input);
            IsInitialized = true;
        }

        public VisionRawModelOutput Execute(Texture inputTexture)
        {
            if (!IsInitialized || worker == null || inputTensor == null)
                throw new InvalidOperationException("Unity inference raw output provider is not initialized.");

            if (inputTexture == null)
                return new VisionRawModelOutput();

            var transform = new TextureTransform().SetTensorLayout(TensorLayout.NCHW);
            TextureConverter.ToTensor(inputTexture, inputTensor, transform);
            worker.Schedule(inputTensor);

            int tensorCount = Mathf.Max(1, profile.output.TensorCount);
            var tensors = new VisionRawTensor[tensorCount];
            for (int i = 0; i < tensorCount; i++)
                tensors[i] = ReadOutputTensor(i);

            return new VisionRawModelOutput { tensors = tensors };
        }

        public void Dispose()
        {
            inputTensor?.Dispose();
            inputTensor = null;
            worker?.Dispose();
            worker = null;
            model = null;
            IsInitialized = false;
        }

        private VisionRawTensor ReadOutputTensor(int index)
        {
            using var outputTensor = index == 0
                ? worker.PeekOutput() as Tensor<float>
                : worker.PeekOutput(index) as Tensor<float>;

            if (outputTensor == null)
                return default;

            using var cpuTensor = outputTensor.ReadbackAndClone() as Tensor<float>;
            if (cpuTensor == null)
                return default;

            var data = new float[cpuTensor.count];
            cpuTensor.AsReadOnlySpan().CopyTo(data);
            VisionTensorSchema schema = profile.output.tensors != null && index < profile.output.tensors.Length
                ? profile.output.tensors[index]
                : default;

            return new VisionRawTensor(
                string.IsNullOrWhiteSpace(schema.name) ? $"output{index}" : schema.name,
                data,
                schema.shape != null && schema.shape.Length > 0 ? schema.shape : new[] { data.Length });
        }

        private static Tensor<float> CreateInputTensor(VisionInputSchema input)
        {
            int width = input.width > 0 ? input.width : 640;
            int height = input.height > 0 ? input.height : 640;
            int channels = input.channels > 0 ? input.channels : 3;
            return new Tensor<float>(new TensorShape(1, channels, height, width));
        }

        private static BackendType ResolveBackend(BackendType backend)
        {
            if (backend == BackendType.GPUCompute && SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D12)
                return BackendType.GPUPixel;

            return backend;
        }
    }
}
