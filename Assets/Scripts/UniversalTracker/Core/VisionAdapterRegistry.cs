using System;
using System.Collections.Generic;
using System.Linq;

namespace UniversalTracker.Core
{
    public sealed class VisionAdapterRegistry
    {
        private readonly List<IVisionModelAdapter> adapters = new List<IVisionModelAdapter>();

        public IReadOnlyList<IVisionModelAdapter> Adapters => adapters;

        public static VisionAdapterRegistry CreateDefault()
        {
            var registry = new VisionAdapterRegistry();
            registry.Register(new YoloModelAdapter());
            return registry;
        }

        public void Register(IVisionModelAdapter adapter)
        {
            if (adapter == null)
                throw new ArgumentNullException(nameof(adapter));

            if (adapters.Any(existing => string.Equals(existing.AdapterId, adapter.AdapterId, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Vision adapter '{adapter.AdapterId}' is already registered.");

            adapters.Add(adapter);
        }

        public bool Unregister(string adapterId)
        {
            if (string.IsNullOrWhiteSpace(adapterId))
                return false;

            int index = adapters.FindIndex(adapter => string.Equals(adapter.AdapterId, adapterId, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
                return false;

            adapters.RemoveAt(index);
            return true;
        }

        public bool TryGetAdapter(VisionModelProfile profile, out IVisionModelAdapter adapter)
        {
            adapter = null;

            if (profile == null)
                return false;

            for (int i = 0; i < adapters.Count; i++)
            {
                if (adapters[i].CanHandle(profile))
                {
                    adapter = adapters[i];
                    return true;
                }
            }

            return false;
        }

        public bool TryCreateRuntime(VisionModelProfile profile, out IVisionRuntimeAdapter runtime, out string error)
        {
            runtime = null;
            error = null;

            if (!TryGetAdapter(profile, out IVisionModelAdapter adapter))
            {
                error = profile == null
                    ? "VisionModelProfile is null."
                    : $"No adapter registered for {profile.family}/{profile.runtimeKind}.";
                return false;
            }

            try
            {
                runtime = adapter.CreateRuntime(profile);
            }
            catch (Exception e)
            {
                error = $"Adapter '{adapter.AdapterId}' failed to create runtime: {e.Message}";
                return false;
            }

            if (runtime == null)
            {
                error = $"Adapter '{adapter.AdapterId}' returned null runtime.";
                return false;
            }

            return true;
        }
    }
}
