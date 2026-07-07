using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;
using UniversalTracker;
using UniversalTracker.Core;

namespace ProAnimaVision.Samples
{
    public sealed partial class ProAnimaVisionExperimentalSceneBootstrap
    {
        private VisualElement videoControlsSection;
        private Label videoPlaylistLabel;
        private Button previousVideoButton;
        private Button nextVideoButton;

        private void EnsureVideoControls()
        {
            VisualElement controlPanel = document.rootVisualElement.Q<VisualElement>("VisionControlPanel");
            if (controlPanel == null)
                return;

            VisualElement existing = controlPanel.Q<VisualElement>("VideoPlaylistControls");
            if (existing != null)
            {
                CacheVideoControls(existing);
                UpdateVideoControls();
                return;
            }

            var section = CreateVideoControlsSection();
            controlPanel.Insert(Mathf.Min(4, controlPanel.childCount), section);
            CacheVideoControls(section);
            UpdateVideoControls();
        }

        public void UseNextVideo()
        {
            SelectPlaylistVideo(1);
        }

        public void UsePreviousVideo()
        {
            SelectPlaylistVideo(-1);
        }

        private void UpdateVideoHotkeys()
        {
            if (realPipelineSource != InputProviderType.Video || !HasSwitchableVideoPlaylist())
                return;

            if (Input.GetKeyDown(KeyCode.LeftArrow))
                UsePreviousVideo();
            else if (Input.GetKeyDown(KeyCode.RightArrow))
                UseNextVideo();
        }

        private VisionVideoPlaylistSource EnsureVideoPlaylist(VideoPlayer player)
        {
            VisionVideoPlaylistSource playlist = ResolveVideoPlaylist(true);
            playlist.videoPlayer = player;
            playlist.ConfigureVideoPlayerDefaults();
            return playlist;
        }

        private VisionVideoPlaylistSource ResolveVideoPlaylist(bool create)
        {
            if (videoPlaylist != null)
                return videoPlaylist;

            videoPlaylist = GetComponent<VisionVideoPlaylistSource>();
            if (videoPlaylist == null && create)
                videoPlaylist = gameObject.AddComponent<VisionVideoPlaylistSource>();

            return videoPlaylist;
        }

        private void SelectPlaylistVideo(int direction)
        {
            VisionVideoPlaylistSource playlist = EnsureVideoPlaylist(EnsureVideoPlayer());
            bool selected = direction < 0
                ? playlist.SelectPrevious(true)
                : playlist.SelectNext(true);

            if (selected && manager != null)
                manager.sourceVideoPlayer = playlist.EnsureVideoPlayer();

            UpdateVideoControls();
        }

        private bool HasSwitchableVideoPlaylist()
        {
            VisionVideoPlaylistSource playlist = ResolveVideoPlaylist(false);
            return playlist != null && playlist.HasVideos && (playlist.wrapNavigation || playlist.ValidCount > 1);
        }

        private VisualElement CreateVideoControlsSection()
        {
            var section = new VisualElement { name = "VideoPlaylistControls" };
            section.style.marginTop = 2;
            section.style.marginBottom = 14;
            section.style.paddingTop = 12;
            section.style.borderTopWidth = 1;
            section.style.borderTopColor = new Color(0.28f, 0.36f, 0.42f, 0.75f);

            var title = new Label("Video");
            title.style.fontSize = 12;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new Color(0.78f, 0.86f, 0.9f, 1f);
            title.style.marginBottom = 6;
            section.Add(title);

            videoPlaylistLabel = new Label { name = "VideoPlaylistLabel" };
            videoPlaylistLabel.style.marginBottom = 8;
            videoPlaylistLabel.style.whiteSpace = WhiteSpace.Normal;
            videoPlaylistLabel.style.color = new Color(0.72f, 0.82f, 0.86f, 1f);
            section.Add(videoPlaylistLabel);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;
            section.Add(row);

            previousVideoButton = CreateSmallButton("<", UsePreviousVideo);
            previousVideoButton.name = "PreviousVideoButton";
            previousVideoButton.tooltip = "Previous video";
            nextVideoButton = CreateSmallButton(">", UseNextVideo);
            nextVideoButton.name = "NextVideoButton";
            nextVideoButton.tooltip = "Next video";
            row.Add(previousVideoButton);
            row.Add(nextVideoButton);
            return section;
        }

        private void CacheVideoControls(VisualElement section)
        {
            videoControlsSection = section;
            videoPlaylistLabel = section.Q<Label>("VideoPlaylistLabel");
            previousVideoButton = section.Q<Button>("PreviousVideoButton");
            nextVideoButton = section.Q<Button>("NextVideoButton");
        }

        private void UpdateVideoControls()
        {
            if (videoControlsSection == null)
                return;

            bool isVideoSource = realPipelineSource == InputProviderType.Video;
            videoControlsSection.style.display = isVideoSource ? DisplayStyle.Flex : DisplayStyle.None;
            if (!isVideoSource)
                return;

            VisionVideoPlaylistSource playlist = ResolveVideoPlaylist(false);
            bool hasPlaylist = playlist != null && playlist.HasVideos;
            if (videoPlaylistLabel != null)
                videoPlaylistLabel.text = hasPlaylist ? CreateVideoPlaylistLabel(playlist) : "Add clips or URLs to VisionVideoPlaylistSource.";

            bool canSwitch = HasSwitchableVideoPlaylist();
            previousVideoButton?.SetEnabled(canSwitch);
            nextVideoButton?.SetEnabled(canSwitch);
        }

        private static string CreateVideoPlaylistLabel(VisionVideoPlaylistSource playlist)
        {
            string label = playlist.CurrentLabel;
            int index = playlist.CurrentIndex + 1;
            return $"{index}/{playlist.Count}: {label}";
        }
    }
}
