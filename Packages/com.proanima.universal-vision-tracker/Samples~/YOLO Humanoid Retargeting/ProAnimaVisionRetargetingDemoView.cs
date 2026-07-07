using UniversalTracker.Core;
using UnityEngine;
using UnityEngine.UI;

namespace UniversalTracker.Samples
{
    internal sealed class ProAnimaVisionRetargetingDemoView
    {
        private readonly Transform parent;
        private readonly int requestedWidth;
        private readonly int requestedHeight;
        private readonly int requestedFps;
        private RawImage previewImage;
        private ProAnimaVisionRetargetingPreviewOverlay overlay;
        private Text statusText;
        private Text metricsText;
        private Texture2D fallbackTexture;
        private Color32[] fallbackPixels;
        private WebCamTexture webCamTexture;

        public ProAnimaVisionRetargetingDemoView(Transform parent, int requestedWidth, int requestedHeight, int requestedFps)
        {
            this.parent = parent;
            this.requestedWidth = requestedWidth;
            this.requestedHeight = requestedHeight;
            this.requestedFps = requestedFps;
        }

        public Texture CurrentTexture => webCamTexture != null && webCamTexture.isPlaying ? webCamTexture : fallbackTexture;

        public Vector2Int SourceSize
        {
            get
            {
                Texture texture = CurrentTexture;
                return texture != null ? new Vector2Int(texture.width, texture.height) : new Vector2Int(requestedWidth, requestedHeight);
            }
        }

        public void Initialize()
        {
            CreateCanvas();
            TryStartWebCam();
            UpdateFallbackTexture(0f);
            UpdatePreviewTexture();
        }

        public void Update(float time, VisionFrameResult result)
        {
            if (webCamTexture == null || !webCamTexture.isPlaying)
                UpdateFallbackTexture(time);

            UpdatePreviewTexture();
            overlay.SetResult(result);
            UpdateLabels(result);
        }

        public void Dispose()
        {
            if (webCamTexture != null)
                webCamTexture.Stop();

            if (fallbackTexture != null)
                Object.Destroy(fallbackTexture);
        }

        private void CreateCanvas()
        {
            var canvasObject = new GameObject("Retargeting Product Test UI");
            canvasObject.transform.SetParent(parent, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();

            RectTransform leftPanel = CreatePanel(canvasObject.transform, "Live Source Preview", 0f, 0.5f);
            RectTransform rightPanel = CreatePanel(canvasObject.transform, "Retargeted Humanoid Rig", 0.5f, 1f);
            CreatePreview(leftPanel);
            CreateText(leftPanel, "SourceLabel", "LIVE SOURCE + BODY SKELETON", 16, TextAnchor.UpperLeft, out statusText);
            CreateText(leftPanel, "MetricsLabel", string.Empty, 12, TextAnchor.LowerLeft, out metricsText);
            CreateText(rightPanel, "RigLabel", "RETARGETED UNITY RIG", 16, TextAnchor.UpperLeft, out _);
        }

        private static RectTransform CreatePanel(Transform root, string name, float anchorMinX, float anchorMaxX)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(root, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(anchorMinX, 0f);
            rect.anchorMax = new Vector2(anchorMaxX, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = panel.AddComponent<Image>();
            image.color = new Color(0.035f, 0.043f, 0.052f, 0.96f);
            return rect;
        }

        private void CreatePreview(RectTransform parentRect)
        {
            var preview = new GameObject("Preview Image");
            preview.transform.SetParent(parentRect, false);
            RectTransform rect = preview.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.04f, 0.09f);
            rect.anchorMax = new Vector2(0.96f, 0.9f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            previewImage = preview.AddComponent<RawImage>();
            previewImage.color = Color.white;

            var overlayObject = new GameObject("Pose Overlay");
            overlayObject.transform.SetParent(preview.transform, false);
            RectTransform overlayRect = overlayObject.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            overlay = overlayObject.AddComponent<ProAnimaVisionRetargetingPreviewOverlay>();
            overlay.raycastTarget = false;
        }

        private static void CreateText(RectTransform parentRect, string name, string text, int fontSize, TextAnchor anchor, out Text label)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parentRect, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = anchor == TextAnchor.LowerLeft ? new Vector2(0.04f, 0.02f) : new Vector2(0.04f, 0.91f);
            rect.anchorMax = anchor == TextAnchor.LowerLeft ? new Vector2(0.96f, 0.08f) : new Vector2(0.96f, 0.98f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            label = textObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = anchor;
            label.color = new Color(0.9f, 0.98f, 0.96f, 1f);
        }

        private void TryStartWebCam()
        {
            WebCamDevice[] devices = WebCamTexture.devices;
            if (devices == null || devices.Length == 0)
                return;

            webCamTexture = new WebCamTexture(devices[0].name, requestedWidth, requestedHeight, requestedFps);
            webCamTexture.Play();
        }

        private void UpdatePreviewTexture()
        {
            if (previewImage != null)
                previewImage.texture = CurrentTexture;
        }

        private void UpdateLabels(VisionFrameResult result)
        {
            string source = webCamTexture != null && webCamTexture.isPlaying ? $"WebCam: {webCamTexture.deviceName}" : "Synthetic video fallback";
            statusText.text = $"{source}  |  COCO-17 pose overlay";
            int poseCount = result.poses?.Length ?? 0;
            int visible = poseCount > 0 ? result.poses[0].VisibleKeypointCount : 0;
            metricsText.text = $"pose={poseCount}  visible={visible}/17  fps target=30";
        }

        private void UpdateFallbackTexture(float time)
        {
            if (fallbackTexture == null)
            {
                fallbackTexture = new Texture2D(640, 360, TextureFormat.RGBA32, false);
                fallbackTexture.name = "Retargeting Demo Synthetic Source";
                fallbackPixels = new Color32[fallbackTexture.width * fallbackTexture.height];
            }

            for (int y = 0; y < fallbackTexture.height; y++)
            {
                for (int x = 0; x < fallbackTexture.width; x++)
                {
                    float u = (float)x / Mathf.Max(1, fallbackTexture.width - 1);
                    float v = (float)y / Mathf.Max(1, fallbackTexture.height - 1);
                    byte r = (byte)Mathf.Lerp(18f, 38f, u);
                    byte g = (byte)Mathf.Lerp(35f, 72f, v);
                    byte b = (byte)(48f + Mathf.Sin((u + time * 0.1f) * 16f) * 18f);
                    fallbackPixels[y * fallbackTexture.width + x] = new Color32(r, g, b, 255);
                }
            }

            fallbackTexture.SetPixels32(fallbackPixels);
            fallbackTexture.Apply(false);
        }
    }
}
