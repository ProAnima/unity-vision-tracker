using NUnit.Framework;
using UnityEngine;
using UnityEngine.Video;
using UniversalTracker.Core;

namespace UniversalTracker.Tests
{
    public sealed class VisionVideoPlaylistSourceTests
    {
        [Test]
        public void ApplyCurrent_WithUrl_AssignsVideoPlayerUrl()
        {
            var go = new GameObject("VisionVideoPlaylistUrlTest");
            var playlist = go.AddComponent<VisionVideoPlaylistSource>();
            playlist.videos = new[]
            {
                new VisionVideoPlaylistItem { displayName = "Walk", url = "file:///walk.mp4" }
            };

            bool applied = playlist.ApplyCurrent(false);
            VideoPlayer player = playlist.EnsureVideoPlayer();

            Assert.That(applied, Is.True);
            Assert.That(player.source, Is.EqualTo(VideoSource.Url));
            Assert.That(player.url, Is.EqualTo("file:///walk.mp4"));
            Assert.That(playlist.CurrentLabel, Is.EqualTo("Walk"));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void SelectNextAndPrevious_WrapValidVideos()
        {
            var go = new GameObject("VisionVideoPlaylistSwitchTest");
            var playlist = go.AddComponent<VisionVideoPlaylistSource>();
            playlist.videos = new[]
            {
                new VisionVideoPlaylistItem { displayName = "First", url = "file:///first.mp4" },
                new VisionVideoPlaylistItem(),
                new VisionVideoPlaylistItem { displayName = "Second", url = "file:///second.mp4" }
            };

            Assert.That(playlist.ApplyCurrent(false), Is.True);
            Assert.That(playlist.SelectNext(false), Is.True);
            Assert.That(playlist.CurrentIndex, Is.EqualTo(2));
            Assert.That(playlist.EnsureVideoPlayer().url, Is.EqualTo("file:///second.mp4"));

            Assert.That(playlist.SelectNext(false), Is.True);
            Assert.That(playlist.CurrentIndex, Is.EqualTo(0));
            Assert.That(playlist.SelectPrevious(false), Is.True);
            Assert.That(playlist.CurrentIndex, Is.EqualTo(2));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void SelectNext_WithoutWrap_StopsAtEnd()
        {
            var go = new GameObject("VisionVideoPlaylistNoWrapTest");
            var playlist = go.AddComponent<VisionVideoPlaylistSource>();
            playlist.wrapNavigation = false;
            playlist.videos = new[]
            {
                new VisionVideoPlaylistItem { url = "file:///only.mp4" }
            };

            Assert.That(playlist.ApplyCurrent(false), Is.True);
            Assert.That(playlist.SelectNext(false), Is.False);
            Assert.That(playlist.CurrentIndex, Is.EqualTo(0));

            Object.DestroyImmediate(go);
        }
    }
}
