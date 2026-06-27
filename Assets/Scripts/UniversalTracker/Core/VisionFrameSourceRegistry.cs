using System;
using System.Collections.Generic;
using UnityEngine;
using UniversalTracker.InputProviders;

namespace UniversalTracker.Core
{
    public sealed class VisionFrameSourceRegistry
    {
        private readonly Dictionary<InputProviderType, VisionFrameSourceDescriptor> descriptors =
            new Dictionary<InputProviderType, VisionFrameSourceDescriptor>();

        public IReadOnlyDictionary<InputProviderType, VisionFrameSourceDescriptor> Descriptors => descriptors;

        public static VisionFrameSourceRegistry CreateDefault()
        {
            var registry = new VisionFrameSourceRegistry();
            registry.Register(InputProviderType.WebCam, VisionFrameSourceType.WebCam, host => host.AddComponent<WebCamInputProvider>());
            registry.Register(InputProviderType.Camera, VisionFrameSourceType.UnityCamera, host => host.AddComponent<CameraInputProvider>());
            registry.Register(InputProviderType.Texture, VisionFrameSourceType.Texture, host => host.AddComponent<TextureInputProvider>());
            registry.Register(InputProviderType.Video, VisionFrameSourceType.Video, host => host.AddComponent<VideoInputProvider>());
            return registry;
        }

        public void Register(InputProviderType inputType, VisionFrameSourceType sourceType, Func<GameObject, IInputProvider> factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            descriptors[inputType] = new VisionFrameSourceDescriptor(inputType, sourceType, factory);
        }

        public bool TryGetSourceType(InputProviderType inputType, out VisionFrameSourceType sourceType)
        {
            sourceType = VisionFrameSourceType.Unknown;

            if (!descriptors.TryGetValue(inputType, out VisionFrameSourceDescriptor descriptor))
                return false;

            sourceType = descriptor.sourceType;
            return true;
        }

        public bool TryCreateProvider(
            InputProviderType inputType,
            GameObject host,
            out IInputProvider provider,
            out VisionFrameSourceType sourceType,
            out string error)
        {
            provider = null;
            sourceType = VisionFrameSourceType.Unknown;
            error = null;

            if (host == null)
            {
                error = "Host GameObject is null.";
                return false;
            }

            if (!descriptors.TryGetValue(inputType, out VisionFrameSourceDescriptor descriptor))
            {
                error = $"No frame source registered for input type '{inputType}'.";
                return false;
            }

            try
            {
                provider = descriptor.CreateProvider(host);
            }
            catch (Exception e)
            {
                error = $"Frame source '{inputType}' failed to create provider: {e.Message}";
                return false;
            }

            if (provider == null)
            {
                error = $"Frame source '{inputType}' returned null provider.";
                return false;
            }

            sourceType = descriptor.sourceType;
            return true;
        }
    }

    public readonly struct VisionFrameSourceDescriptor
    {
        private readonly Func<GameObject, IInputProvider> factory;

        public VisionFrameSourceDescriptor(
            InputProviderType inputType,
            VisionFrameSourceType sourceType,
            Func<GameObject, IInputProvider> factory)
        {
            this.inputType = inputType;
            this.sourceType = sourceType;
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public readonly InputProviderType inputType;
        public readonly VisionFrameSourceType sourceType;

        public IInputProvider CreateProvider(GameObject host)
        {
            return factory(host);
        }
    }
}
