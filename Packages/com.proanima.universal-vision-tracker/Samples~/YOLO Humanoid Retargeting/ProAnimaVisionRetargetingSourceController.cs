using System.Collections.Generic;
using UniversalTracker.Core;
using UnityEngine;
using UnityEngine.Video;

namespace UniversalTracker.Samples
{
    public enum ProAnimaVisionRetargetingSourceMode
    {
        Camera,
        Video,
        Synthetic
    }

    internal sealed class ProAnimaVisionRetargetingSourceController
    {
        private const string DefaultCameraLabel = "Default Camera";
        private readonly MonoBehaviour owner;
        private readonly int requestedWidth;
        private readonly int requestedHeight;
        private readonly int requestedFps;
        private readonly List<string> cameraChoices = new List<string>();
        private readonly List<string> videoChoices = new List<string>();
        private IVisionFrameSource frameSource;
        private Texture2D fallbackTexture;
        private Color32[] fallbackPixels;
        private VisionFrame lastFrame;

        public ProAnimaVisionRetargetingSourceController(
            MonoBehaviour owner,
            ProAnimaVisionRetargetingSourceMode mode,
            int cameraDeviceIndex,
            string cameraDeviceName,
            int requestedWidth,
            int requestedHeight,
            int requestedFps,
            VideoPlayer videoPlayer,
            VisionVideoPlaylistSource videoPlaylist)
        {
            this.owner = owner;
            Mode = mode;
            CameraDeviceIndex = cameraDeviceIndex;
            CameraDeviceName = cameraDeviceName;
            this.requestedWidth = requestedWidth;
            this.requestedHeight = requestedHeight;
            this.requestedFps = requestedFps;
            SourceVideoPlayer = videoPlayer;
            VideoPlaylist = videoPlaylist;
        }

        public ProAnimaVisionRetargetingSourceMode Mode { get; private set; }
        public int CameraDeviceIndex { get; private set; }
        public string CameraDeviceName { get; private set; }
        public VideoPlayer SourceVideoPlayer { get; private set; }
        public VisionVideoPlaylistSource VideoPlaylist { get; private set; }
        public string Status { get; private set; } = "Initializing source";
        public Texture CurrentTexture => lastFrame.texture != null ? lastFrame.texture : fallbackTexture;
        public Vector2Int SourceSize => lastFrame.IsValid ? lastFrame.sourceSize : new Vector2Int(requestedWidth, requestedHeight);
        public IReadOnlyList<string> CameraChoices => cameraChoices;
        public IReadOnlyList<string> VideoChoices => videoChoices;

        public void Initialize()
        {
            RefreshCameraChoices();
            RefreshVideoChoices();
            EnsureFallbackTexture();
            SetMode(Mode);
        }

        public void Dispose()
        {
            DisposeFrameSource();
            if (fallbackTexture != null)
                Object.Destroy(fallbackTexture);
        }

        public void SetMode(ProAnimaVisionRetargetingSourceMode mode)
        {
            Mode = mode;
            DisposeFrameSource();

            if (mode == ProAnimaVisionRetargetingSourceMode.Camera)
                StartCameraSource();
            else if (mode == ProAnimaVisionRetargetingSourceMode.Video)
                StartVideoSource();
            else
                Status = "Synthetic source";
        }

        public bool TryUpdate(float time, out VisionFrame frame)
        {
            frame = default;
            if (Mode != ProAnimaVisionRetargetingSourceMode.Synthetic &&
                frameSource != null &&
                frameSource.TryGetFrame(out frame))
            {
                lastFrame = frame;
                Status = CreateRunningStatus(frame);
                return true;
            }

            UpdateFallbackTexture(time);
            frame = VisionFrame.FromTexture(fallbackTexture, Time.frameCount, Time.realtimeSinceStartupAsDouble);
            lastFrame = frame;
            if (Mode != ProAnimaVisionRetargetingSourceMode.Synthetic)
                Status = $"{Mode} waiting for frames - synthetic fallback";

            return frame.IsValid;
        }

        public void RefreshCameraChoices()
        {
            cameraChoices.Clear();
            cameraChoices.Add(DefaultCameraLabel);
            WebCamDevice[] devices = WebCamTexture.devices;
            if (devices == null)
                return;

            for (int i = 0; i < devices.Length; i++)
                cameraChoices.Add(devices[i].name);
        }

        public string ResolveCameraChoice()
        {
            string deviceName = ResolveDeviceName();
            if (!string.IsNullOrWhiteSpace(deviceName) && cameraChoices.Contains(deviceName))
                return deviceName;

            return cameraChoices.Count > 0 ? cameraChoices[0] : DefaultCameraLabel;
        }

        public void SelectCamera(string choice)
        {
            if (string.IsNullOrWhiteSpace(choice))
                return;

            CameraDeviceName = choice == DefaultCameraLabel ? null : choice;
            WebCamDevice[] devices = WebCamTexture.devices;
            if (devices != null)
            {
                for (int i = 0; i < devices.Length; i++)
                {
                    if (devices[i].name != CameraDeviceName)
                        continue;

                    CameraDeviceIndex = i;
                    break;
                }
            }

            if (Mode == ProAnimaVisionRetargetingSourceMode.Camera)
                SetMode(Mode);
        }

        public void UseNextCamera()
        {
            WebCamDevice[] devices = WebCamTexture.devices;
            if (devices == null || devices.Length == 0)
                return;

            CameraDeviceIndex = (CameraDeviceIndex + 1) % devices.Length;
            CameraDeviceName = devices[CameraDeviceIndex].name;
            RefreshCameraChoices();
            if (Mode == ProAnimaVisionRetargetingSourceMode.Camera)
                SetMode(Mode);
        }

        public void RefreshVideoChoices()
        {
            videoChoices.Clear();
            VisionVideoPlaylistSource playlist = ResolveVideoPlaylist(false);
            if (playlist == null || playlist.Count == 0)
            {
                videoChoices.Add("No playlist videos configured");
                return;
            }

            for (int i = 0; i < playlist.Count; i++)
                videoChoices.Add($"{i + 1}/{playlist.Count}: {GetVideoLabel(playlist, i)}");
        }

        public int CurrentVideoChoiceIndex()
        {
            VisionVideoPlaylistSource playlist = ResolveVideoPlaylist(false);
            return playlist != null && playlist.Count > 0 ? Mathf.Clamp(playlist.CurrentIndex, 0, videoChoices.Count - 1) : 0;
        }

        public bool SelectVideo(int index)
        {
            VisionVideoPlaylistSource playlist = ResolveVideoPlaylist(true);
            if (playlist == null || !playlist.Select(index, true))
                return false;

            SourceVideoPlayer = playlist.EnsureVideoPlayer();
            RefreshVideoChoices();
            if (Mode == ProAnimaVisionRetargetingSourceMode.Video)
                SetMode(Mode);

            return true;
        }

        public bool UseNextVideo()
        {
            VisionVideoPlaylistSource playlist = ResolveVideoPlaylist(true);
            if (playlist == null || !playlist.SelectNext(true))
                return false;

            SourceVideoPlayer = playlist.EnsureVideoPlayer();
            RefreshVideoChoices();
            if (Mode == ProAnimaVisionRetargetingSourceMode.Video)
                SetMode(Mode);

            return true;
        }

        public bool UsePreviousVideo()
        {
            VisionVideoPlaylistSource playlist = ResolveVideoPlaylist(true);
            if (playlist == null || !playlist.SelectPrevious(true))
                return false;

            SourceVideoPlayer = playlist.EnsureVideoPlayer();
            RefreshVideoChoices();
            if (Mode == ProAnimaVisionRetargetingSourceMode.Video)
                SetMode(Mode);

            return true;
        }

        public bool HasSwitchableVideo()
        {
            VisionVideoPlaylistSource playlist = ResolveVideoPlaylist(false);
            return playlist != null && playlist.HasVideos && (playlist.wrapNavigation || playlist.ValidCount > 1);
        }

        private void StartCameraSource()
        {
            string deviceName = ResolveDeviceName();
            frameSource = new WebCamFrameSource(deviceName, requestedWidth, requestedHeight, requestedFps);
            frameSource.Initialize();
            Status = string.IsNullOrWhiteSpace(deviceName) ? "Default camera starting" : $"Camera starting: {deviceName}";
        }

        private void StartVideoSource()
        {
            VideoPlayer player = EnsureVideoPlayer();
            VisionVideoPlaylistSource playlist = ResolveVideoPlaylist(true);
            if (playlist != null && playlist.HasVideos)
            {
                playlist.videoPlayer = player;
                playlist.ConfigureVideoPlayerDefaults();
                playlist.ApplyCurrent(true);
            }

            frameSource = new VideoFrameSource(player, true);
            frameSource.Initialize();
            Status = playlist != null && playlist.HasVideos ? $"Video starting: {playlist.CurrentLabel}" : "Video source waiting for a clip or URL";
        }

        private string ResolveDeviceName()
        {
            if (!string.IsNullOrWhiteSpace(CameraDeviceName))
                return CameraDeviceName;

            WebCamDevice[] devices = WebCamTexture.devices;
            if (devices == null || devices.Length == 0)
                return null;

            int index = Mathf.Clamp(CameraDeviceIndex, 0, devices.Length - 1);
            return devices[index].name;
        }

        private VideoPlayer EnsureVideoPlayer()
        {
            if (SourceVideoPlayer == null)
                SourceVideoPlayer = owner.GetComponent<VideoPlayer>() ?? owner.gameObject.AddComponent<VideoPlayer>();

            VisionVideoPlaylistSource.ConfigureVideoPlayerDefaults(SourceVideoPlayer);
            return SourceVideoPlayer;
        }

        private VisionVideoPlaylistSource ResolveVideoPlaylist(bool create)
        {
            if (VideoPlaylist != null)
                return VideoPlaylist;

            VideoPlaylist = owner.GetComponent<VisionVideoPlaylistSource>();
            if (VideoPlaylist == null && create)
                VideoPlaylist = owner.gameObject.AddComponent<VisionVideoPlaylistSource>();

            if (VideoPlaylist != null)
                VideoPlaylist.videoPlayer = EnsureVideoPlayer();

            return VideoPlaylist;
        }

        private static string GetVideoLabel(VisionVideoPlaylistSource playlist, int index)
        {
            if (playlist.videos == null || index < 0 || index >= playlist.videos.Length)
                return "Unassigned video";

            return playlist.videos[index].Label;
        }

        private void DisposeFrameSource()
        {
            frameSource?.Dispose();
            frameSource = null;
            lastFrame = default;
        }

        private static string CreateRunningStatus(VisionFrame frame)
        {
            return $"{frame.sourceType} {frame.sourceSize.x}x{frame.sourceSize.y}";
        }

        private void EnsureFallbackTexture()
        {
            if (fallbackTexture != null)
                return;

            fallbackTexture = new Texture2D(640, 360, TextureFormat.RGBA32, false);
            fallbackTexture.name = "Retargeting Demo Synthetic Source";
            fallbackPixels = new Color32[fallbackTexture.width * fallbackTexture.height];
        }

        private void UpdateFallbackTexture(float time)
        {
            EnsureFallbackTexture();
            for (int y = 0; y < fallbackTexture.height; y++)
            {
                for (int x = 0; x < fallbackTexture.width; x++)
                {
                    float u = (float)x / Mathf.Max(1, fallbackTexture.width - 1);
                    float v = (float)y / Mathf.Max(1, fallbackTexture.height - 1);
                    byte r = (byte)Mathf.Lerp(18f, 42f, u);
                    byte g = (byte)Mathf.Lerp(34f, 78f, v);
                    byte b = (byte)(52f + Mathf.Sin((u + time * 0.11f) * 16f) * 18f);
                    fallbackPixels[y * fallbackTexture.width + x] = new Color32(r, g, b, 255);
                }
            }

            fallbackTexture.SetPixels32(fallbackPixels);
            fallbackTexture.Apply(false);
        }
    }
}
