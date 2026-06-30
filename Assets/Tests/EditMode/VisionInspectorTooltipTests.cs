using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UniversalTracker.Core;
using UniversalTracker.OutputReceivers;

namespace UniversalTracker.Tests
{
    public sealed class VisionInspectorTooltipTests
    {
        private static readonly Type[] InspectorFacingTypes =
        {
            typeof(UniversalTrackerManager),
            typeof(UniversalTrackerConfig),
            typeof(SafetyConfig),
            typeof(VisionModelProfile),
            typeof(VisionInputSchema),
            typeof(VisionOutputSchema),
            typeof(VisionTensorSchema),
            typeof(VisionOutputCoordinateTransform),
            typeof(VisionPipelineProfile),
            typeof(VisionStagePerformanceBudget),
            typeof(VisionPerformanceBudget),
            typeof(VisionToolkitDashboardReceiver),
            typeof(UIVisualizationReceiver),
            typeof(DebugOutputReceiver),
            typeof(EventOutputReceiver),
            typeof(SceneVisualizationReceiver)
        };

        [Test]
        public void InspectorFacingRuntimeFields_HaveTooltips()
        {
            var missing = new List<string>();

            foreach (Type type in InspectorFacingTypes)
            {
                foreach (FieldInfo field in GetInspectorFields(type))
                {
                    if (field.GetCustomAttribute<TooltipAttribute>() == null)
                        missing.Add($"{type.Name}.{field.Name}");
                }
            }

            Assert.That(missing, Is.Empty, "Add TooltipAttribute to inspector-facing fields: " + string.Join(", ", missing));
        }

        private static IEnumerable<FieldInfo> GetInspectorFields(Type type)
        {
            return type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(field => !field.IsStatic)
                .Where(field => !field.IsInitOnly)
                .Where(field => !field.IsLiteral)
                .Where(field => field.GetCustomAttribute<NonSerializedAttribute>() == null)
                .Where(field => !field.Name.Contains("k__BackingField"))
                .Where(field => field.IsPublic || field.GetCustomAttribute<SerializeField>() != null);
        }
    }
}
