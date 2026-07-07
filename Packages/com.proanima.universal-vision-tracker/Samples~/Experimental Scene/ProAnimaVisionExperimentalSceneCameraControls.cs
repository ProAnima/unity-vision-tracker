using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UniversalTracker;

namespace ProAnimaVision.Samples
{
    public sealed partial class ProAnimaVisionExperimentalSceneBootstrap
    {
        private VisualElement cameraControlsSection;

        private void EnsureCameraControls()
        {
            VisualElement controlPanel = document.rootVisualElement.Q<VisualElement>("VisionControlPanel");
            if (controlPanel == null)
                return;

            VisualElement existing = controlPanel.Q<VisualElement>("WebCamControls");
            if (existing != null)
            {
                cameraControlsSection = existing;
                UpdateCameraControls();
                return;
            }

            var section = new VisualElement { name = "WebCamControls" };
            cameraControlsSection = section;
            section.style.marginTop = 2;
            section.style.marginBottom = 14;
            section.style.paddingTop = 12;
            section.style.borderTopWidth = 1;
            section.style.borderTopColor = new Color(0.28f, 0.36f, 0.42f, 0.75f);

            var title = new Label("Camera");
            title.style.fontSize = 12;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new Color(0.78f, 0.86f, 0.9f, 1f);
            title.style.marginBottom = 6;
            section.Add(title);

            cameraDropdown = new DropdownField();
            cameraDropdown.style.marginBottom = 8;
            cameraDropdown.RegisterValueChangedCallback(evt => SelectCamera(evt.newValue));
            section.Add(cameraDropdown);
            RefreshCameraDropdown();

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;
            section.Add(row);

            row.Add(CreateSmallButton("Next", UseNextWebCam));
            row.Add(CreateSmallButton("Rotate", RotatePreview));
            row.Add(CreateSmallButton(mirrorPreviewX ? "Mirror On" : "Mirror", ToggleMirror));

            controlPanel.Insert(Mathf.Min(3, controlPanel.childCount), section);
            UpdateCameraControls();
        }

        private Button CreateSmallButton(string text, Action action)
        {
            var button = new Button(() => action?.Invoke()) { text = text };
            button.style.height = 28;
            button.style.minWidth = 78;
            button.style.flexGrow = 1f;
            button.style.marginRight = 6;
            button.style.marginBottom = 6;
            button.style.borderTopLeftRadius = 5;
            button.style.borderTopRightRadius = 5;
            button.style.borderBottomLeftRadius = 5;
            button.style.borderBottomRightRadius = 5;
            button.style.backgroundColor = new Color(0.12f, 0.16f, 0.18f, 1f);
            button.style.color = new Color(0.86f, 0.92f, 0.95f, 1f);
            return button;
        }

        private void RefreshCameraDropdown()
        {
            if (cameraDropdown == null)
                return;

            List<string> choices = GetCameraChoices();
            cameraDropdown.choices = choices;
            cameraDropdown.SetValueWithoutNotify(ResolveCameraDropdownValue(choices));
        }

        private List<string> GetCameraChoices()
        {
            WebCamDevice[] devices = WebCamTexture.devices;
            var choices = new List<string> { "Default Camera" };
            if (devices == null || devices.Length == 0)
                return choices;

            for (int i = 0; i < devices.Length; i++)
                choices.Add(devices[i].name);

            return choices;
        }

        private string ResolveCameraDropdownValue(List<string> choices)
        {
            string deviceName = ResolveDeviceName();
            if (!string.IsNullOrWhiteSpace(deviceName) && choices.Contains(deviceName))
                return deviceName;

            return choices[0];
        }

        private void SelectCamera(string selected)
        {
            if (string.IsNullOrWhiteSpace(selected))
                return;

            deviceNameOverride = selected == "Default Camera" ? null : selected;
            WebCamDevice[] devices = WebCamTexture.devices;
            if (devices != null)
            {
                for (int i = 0; i < devices.Length; i++)
                {
                    if (devices[i].name == deviceNameOverride)
                    {
                        deviceIndex = i;
                        break;
                    }
                }
            }

            RestartWebCamPreview();
            UpdateManagerCameraSettings();
            RestartPipelineIfNeeded();
        }

        private void RotatePreview()
        {
            previewRotationDegrees = (previewRotationDegrees + 90) % 360;
            ApplyDashboardPreviewSettings();
        }

        private void ToggleMirror()
        {
            mirrorPreviewX = !mirrorPreviewX;
            ApplyDashboardPreviewSettings();
        }

        private void UpdateManagerCameraSettings()
        {
            if (manager == null)
                return;

            manager.webCamDeviceName = ResolveDeviceName();
            manager.webCamRequestedWidth = requestedWidth;
            manager.webCamRequestedHeight = requestedHeight;
            manager.webCamRequestedFps = requestedFps;
        }

        private void UpdateCameraControls()
        {
            if (cameraControlsSection == null)
                return;

            bool isCameraSource = realPipelineSource == InputProviderType.WebCam;
            cameraControlsSection.style.display = isCameraSource ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void RestartPipelineIfNeeded()
        {
            if (!configureRealPipeline || manager == null || !manager.IsRunning)
                return;

            manager.StopTracking();
            manager.StartTracking();
        }
    }
}
