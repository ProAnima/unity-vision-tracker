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
            registry.Register(new YoloPose2DOutputParser());
            registry.Register(new YoloSegmentationOutputParser());
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
            return TryGetParser(profile, out parser, out _, out _);
        }

        public bool TryGetParser(
            VisionModelProfile profile,
            out IVisionOutputParser parser,
            out string diagnosticCode,
            out string diagnosticMessage)
        {
            parser = null;
            diagnosticCode = null;
            diagnosticMessage = null;

            if (profile == null)
            {
                diagnosticCode = "parser.profile.null";
                diagnosticMessage = "VisionModelProfile is null.";
                return false;
            }

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

                diagnosticCode = "parser.id.not_registered";
                diagnosticMessage = $"No registered output parser matches parserId '{profile.parserId}'.";
                return false;
            }

            for (int i = 0; i < parsers.Count; i++)
            {
                if (parsers[i].CanParse(profile))
                {
                    parser = parsers[i];
                    return true;
                }
            }

            diagnosticCode = "parser.compatibility.none";
            diagnosticMessage = $"No registered output parser can parse family '{profile.family}' with capabilities '{profile.capabilities}'.";
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
