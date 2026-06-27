using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniversalTracker.Core
{
    public sealed class VisionOutputParserRegistry
    {
        private readonly List<IVisionOutputParser> parsers = new List<IVisionOutputParser>();

        public IReadOnlyList<IVisionOutputParser> Parsers => parsers;

        public static VisionOutputParserRegistry CreateDefault()
        {
            var registry = new VisionOutputParserRegistry();
            registry.Register(new YoloDetectionOutputParser());
            return registry;
        }

        public void Register(IVisionOutputParser parser)
        {
            if (parser == null)
                throw new ArgumentNullException(nameof(parser));

            for (int i = 0; i < parsers.Count; i++)
            {
                if (string.Equals(parsers[i].ParserId, parser.ParserId, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Vision output parser '{parser.ParserId}' is already registered.");
            }

            parsers.Add(parser);
        }

        public bool TryGetParser(VisionModelProfile profile, out IVisionOutputParser parser)
        {
            parser = null;

            if (profile == null)
                return false;

            if (!string.IsNullOrWhiteSpace(profile.parserId))
            {
                for (int i = 0; i < parsers.Count; i++)
                {
                    if (string.Equals(parsers[i].ParserId, profile.parserId, StringComparison.OrdinalIgnoreCase))
                    {
                        parser = parsers[i];
                        return true;
                    }
                }
            }

            for (int i = 0; i < parsers.Count; i++)
            {
                if (parsers[i].CanParse(profile))
                {
                    parser = parsers[i];
                    return true;
                }
            }

            return false;
        }
    }

    public interface IVisionRawOutputProvider : IDisposable
    {
        bool IsInitialized { get; }
        void Initialize(VisionModelProfile profile);
        VisionRawModelOutput Execute(Texture inputTexture);
    }
}
