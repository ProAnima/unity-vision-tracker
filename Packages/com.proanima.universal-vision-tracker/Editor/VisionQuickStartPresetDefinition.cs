using System;
using System.Collections.Generic;

namespace UniversalTracker.Editor
{
    internal enum VisionQuickStartPreset
    {
        WebCamPreview,
        YoloDetectionWebCam,
        YoloPoseWebCam,
        YoloSegmentationWebCam,
        YoloDetectionVideo,
        YoloPoseVideo,
        YoloSegmentationVideo
    }

    internal sealed class VisionQuickStartPresetDefinition
    {
        public static readonly VisionQuickStartPresetDefinition[] All =
        {
            new(
                VisionQuickStartPreset.WebCamPreview,
                "WebCam Preview",
                "Open the demo scene with live camera preview, fit, rotation, and mirror controls.",
                "No model required",
                "Import/Open Scene"),
            new(
                VisionQuickStartPreset.YoloDetectionWebCam,
                "YOLO Detection + WebCam",
                "Create detection model and pipeline profiles, wire the demo scene, and auto-start when the model asset is available.",
                "Finds Assets/Models/yolo26n.onnx first",
                "Apply Preset"),
            new(
                VisionQuickStartPreset.YoloPoseWebCam,
                "YOLO Pose + WebCam",
                "Create pose model and pipeline profiles, wire the demo scene, and show skeleton overlay output.",
                "Finds Assets/Models/yolo26n-pose.onnx first",
                "Apply Preset"),
            new(
                VisionQuickStartPreset.YoloSegmentationWebCam,
                "YOLO Segmentation + WebCam",
                "Create segmentation model and pipeline profiles, wire the demo scene, and enable mask overlay output.",
                "Finds Assets/Models/yolo26n-seg.onnx first",
                "Apply Preset"),
            new(
                VisionQuickStartPreset.YoloDetectionVideo,
                "YOLO Detection + Video",
                "Create detection profiles, wire the demo scene to a VideoPlayer source, and let you assign a clip or URL instead of using a webcam.",
                "Assign a Video Clip or URL on the scene VideoPlayer",
                "Apply Preset"),
            new(
                VisionQuickStartPreset.YoloPoseVideo,
                "YOLO Pose + Video",
                "Create pose profiles, wire the demo scene to a VideoPlayer source, and show stabilized skeleton overlay output on file/video playback.",
                "Assign a Video Clip or URL on the scene VideoPlayer",
                "Apply Preset"),
            new(
                VisionQuickStartPreset.YoloSegmentationVideo,
                "YOLO Segmentation + Video",
                "Create segmentation profiles, wire the demo scene to a VideoPlayer source, and render mask contours on file/video playback.",
                "Assign a Video Clip or URL on the scene VideoPlayer",
                "Apply Preset")
        };

        private static readonly Dictionary<VisionQuickStartPreset, VisionQuickStartPresetDefinition> ByPreset = CreateLookup();
        private static readonly Dictionary<string, VisionQuickStartPresetDefinition> ByLabel = CreateLabelLookup();

        public VisionQuickStartPresetDefinition(
            VisionQuickStartPreset preset,
            string label,
            string description,
            string requirement,
            string actionLabel)
        {
            Preset = preset;
            Label = label;
            Description = description;
            Requirement = requirement;
            ActionLabel = actionLabel;
        }

        public VisionQuickStartPreset Preset { get; }
        public string Label { get; }
        public string Description { get; }
        public string Requirement { get; }
        public string ActionLabel { get; }

        public static VisionQuickStartPresetDefinition Get(VisionQuickStartPreset preset)
        {
            return ByPreset[preset];
        }

        public static VisionQuickStartPresetDefinition FromLabel(string label)
        {
            if (!string.IsNullOrWhiteSpace(label) && ByLabel.TryGetValue(label, out VisionQuickStartPresetDefinition definition))
                return definition;

            return All[0];
        }

        public static List<string> Labels()
        {
            var labels = new List<string>(All.Length);
            foreach (VisionQuickStartPresetDefinition definition in All)
                labels.Add(definition.Label);
            return labels;
        }

        private static Dictionary<VisionQuickStartPreset, VisionQuickStartPresetDefinition> CreateLookup()
        {
            var lookup = new Dictionary<VisionQuickStartPreset, VisionQuickStartPresetDefinition>();
            foreach (VisionQuickStartPresetDefinition definition in All)
                lookup.Add(definition.Preset, definition);
            return lookup;
        }

        private static Dictionary<string, VisionQuickStartPresetDefinition> CreateLabelLookup()
        {
            var lookup = new Dictionary<string, VisionQuickStartPresetDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (VisionQuickStartPresetDefinition definition in All)
                lookup.Add(definition.Label, definition);
            return lookup;
        }
    }
}
