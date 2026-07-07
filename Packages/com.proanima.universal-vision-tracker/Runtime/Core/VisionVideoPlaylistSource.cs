using System;
using UnityEngine;
using UnityEngine.Video;

namespace UniversalTracker.Core
{
    [Serializable]
    public struct VisionVideoPlaylistItem
    {
        [Tooltip("Optional label shown by sample controls. Uses clip name or URL when empty.")]
        public string displayName;
        [Tooltip("Project video clip. Takes priority over URL when assigned.")]
        public VideoClip clip;
        [Tooltip("File path or streaming URL used when no clip is assigned.")]
        public string url;

        public bool IsValid => clip != null || !string.IsNullOrWhiteSpace(url);

        public string Label
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(displayName))
                    return displayName.Trim();

                if (clip != null)
                    return clip.name;

                return !string.IsNullOrWhiteSpace(url) ? url.Trim() : "Unassigned video";
            }
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(VideoPlayer))]
    public sealed class VisionVideoPlaylistSource : MonoBehaviour
    {
        [Tooltip("VideoPlayer controlled by this playlist. Uses the local component when empty.")]
        public VideoPlayer videoPlayer;
        [Tooltip("Ordered video row. Add Video Clips, file URLs, or streaming URLs here.")]
        public VisionVideoPlaylistItem[] videos = Array.Empty<VisionVideoPlaylistItem>();
        [Tooltip("Currently selected video index in the playlist.")]
        [Min(0)] public int activeVideoIndex;
        [Tooltip("Wrap previous/next navigation at the ends of the playlist.")]
        public bool wrapNavigation = true;
        [Tooltip("Play the selected video after runtime previous/next switching.")]
        public bool playOnSwitch = true;
        [Tooltip("Prepare the selected video when the component awakens.")]
        public bool prepareOnAwake = true;

        private bool playWhenPrepared;

        public int Count => videos?.Length ?? 0;
        public int ValidCount => CountValidVideos();
        public bool HasVideos => ValidCount > 0;
        public int CurrentIndex => Count == 0 ? -1 : Mathf.Clamp(activeVideoIndex, 0, Count - 1);
        public string CurrentLabel => TryGetCurrentItem(out VisionVideoPlaylistItem item) ? item.Label : "No video";

        private void Awake()
        {
            EnsureVideoPlayer();
            ClampActiveIndex();
            if (prepareOnAwake)
                ApplyCurrent(false);
        }

        private void OnEnable()
        {
            VideoPlayer player = EnsureVideoPlayer();
            player.prepareCompleted -= HandlePrepared;
            player.prepareCompleted += HandlePrepared;
        }

        private void OnDisable()
        {
            if (videoPlayer != null)
                videoPlayer.prepareCompleted -= HandlePrepared;

            playWhenPrepared = false;
        }

        private void OnValidate()
        {
            ClampActiveIndex();
            if (videoPlayer == null)
                videoPlayer = GetComponent<VideoPlayer>();
        }

        public VideoPlayer EnsureVideoPlayer()
        {
            if (videoPlayer == null)
                videoPlayer = GetComponent<VideoPlayer>() ?? gameObject.AddComponent<VideoPlayer>();

            ConfigureVideoPlayerDefaults(videoPlayer);
            return videoPlayer;
        }

        public void ConfigureVideoPlayerDefaults()
        {
            ConfigureVideoPlayerDefaults(EnsureVideoPlayer());
        }

        public static void ConfigureVideoPlayerDefaults(VideoPlayer player)
        {
            if (player == null)
                return;

            player.playOnAwake = false;
            player.waitForFirstFrame = true;
            player.isLooping = true;
            player.renderMode = VideoRenderMode.APIOnly;
        }

        public bool ApplyCurrent(bool play = false)
        {
            if (!TryGetCurrentItem(out VisionVideoPlaylistItem item))
                return false;

            return ApplyItem(item, play && playOnSwitch);
        }

        public bool Select(int index, bool play = true)
        {
            if (!IsValidIndex(index))
                return false;

            activeVideoIndex = index;
            return ApplyCurrent(play);
        }

        public bool SelectNext(bool play = true)
        {
            return Move(1, play);
        }

        public bool SelectPrevious(bool play = true)
        {
            return Move(-1, play);
        }

        public bool Next()
        {
            return SelectNext(true);
        }

        public bool Previous()
        {
            return SelectPrevious(true);
        }

        public bool TryGetCurrentItem(out VisionVideoPlaylistItem item)
        {
            if (TryResolveValidIndex(activeVideoIndex, 1, out int resolvedIndex))
            {
                activeVideoIndex = resolvedIndex;
                item = videos[resolvedIndex];
                return true;
            }

            item = default;
            return false;
        }

        private bool ApplyItem(VisionVideoPlaylistItem item, bool play)
        {
            VideoPlayer player = EnsureVideoPlayer();
            playWhenPrepared = false;
            if (player.isPlaying)
                player.Stop();

            if (item.clip != null)
            {
                player.source = VideoSource.VideoClip;
                player.clip = item.clip;
                player.url = string.Empty;
            }
            else
            {
                player.source = VideoSource.Url;
                player.clip = null;
                player.url = item.url.Trim();
            }

            if (!Application.isPlaying)
                return true;

            playWhenPrepared = play;
            player.Prepare();
            return true;
        }

        private bool Move(int direction, bool play)
        {
            if (Count == 0)
                return false;

            int start = Mathf.Clamp(activeVideoIndex, 0, Count - 1);
            for (int step = 1; step <= Count; step++)
            {
                int rawIndex = start + step * direction;
                if (!wrapNavigation && (rawIndex < 0 || rawIndex >= Count))
                    return false;

                int candidate = wrapNavigation ? WrapIndex(rawIndex, Count) : rawIndex;
                if (IsValidIndex(candidate))
                    return Select(candidate, play);
            }

            return false;
        }

        private bool TryResolveValidIndex(int preferredIndex, int direction, out int resolvedIndex)
        {
            resolvedIndex = -1;
            if (Count == 0)
                return false;

            int start = Mathf.Clamp(preferredIndex, 0, Count - 1);
            if (IsValidIndex(start))
            {
                resolvedIndex = start;
                return true;
            }

            int stepDirection = direction < 0 ? -1 : 1;
            for (int step = 1; step < Count; step++)
            {
                int candidate = WrapIndex(start + step * stepDirection, Count);
                if (!IsValidIndex(candidate))
                    continue;

                resolvedIndex = candidate;
                return true;
            }

            return false;
        }

        private bool IsValidIndex(int index)
        {
            return videos != null && index >= 0 && index < videos.Length && videos[index].IsValid;
        }

        private int CountValidVideos()
        {
            if (videos == null)
                return 0;

            int count = 0;
            for (int i = 0; i < videos.Length; i++)
            {
                if (videos[i].IsValid)
                    count++;
            }

            return count;
        }

        private void ClampActiveIndex()
        {
            if (activeVideoIndex < 0)
                activeVideoIndex = 0;

            if (videos != null && videos.Length > 0)
                activeVideoIndex = Mathf.Min(activeVideoIndex, videos.Length - 1);
        }

        private static int WrapIndex(int index, int count)
        {
            return ((index % count) + count) % count;
        }

        private void HandlePrepared(VideoPlayer source)
        {
            if (!playWhenPrepared || source == null || source != videoPlayer)
                return;

            playWhenPrepared = false;
            if (!source.isPlaying)
                source.Play();
        }
    }
}
