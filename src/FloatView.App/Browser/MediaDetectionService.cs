using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace FloatView.App.Browser;

public record MediaPlaybackInfo(
    bool HasMedia,
    bool IsPlaying,
    double CurrentTime,
    double Duration,
    string Title,
    double Volume,
    bool IsMuted,
    bool SupportsNativePip,
    string CurrentCaption);

public class MediaDetectionService
{
    public const string InjectedScript = @"
(function() {
    // Deep recursive selector that pierces Shadow DOM boundaries
    function deepQueryAll(selector, root) {
        root = root || document;
        var results = [];
        try {
            if (root.querySelectorAll) {
                var direct = root.querySelectorAll(selector);
                for (var i = 0; i < direct.length; i++) results.push(direct[i]);
            }
            var walker = document.createTreeWalker(root, NodeFilter.SHOW_ELEMENT, null, false);
            while (walker.nextNode()) {
                var el = walker.currentNode;
                if (el.shadowRoot) {
                    var shadowMatches = deepQueryAll(selector, el.shadowRoot);
                    for (var j = 0; j < shadowMatches.length; j++) results.push(shadowMatches[j]);
                }
            }
        } catch (e) {}
        return results;
    }

    // Unblock Picture-in-Picture restrictions across elements and iframes
    function unblockPictureInPicture() {
        try {
            var iframes = document.querySelectorAll('iframe');
            for (var i = 0; i < iframes.length; i++) {
                var f = iframes[i];
                var allow = f.getAttribute('allow') || '';
                if (!allow.includes('picture-in-picture')) {
                    f.setAttribute('allow', (allow ? allow + '; ' : '') + 'picture-in-picture; autoplay');
                }
            }
        } catch (e) {}
    }

    // Platform-Specific Player API detection
    function getYouTubePlayer() {
        try {
            var el = document.getElementById('movie_player');
            if (el && typeof el.getCurrentTime === 'function') return el;
        } catch (e) {}
        return null;
    }

    function getNetflixPlayer() {
        try {
            if (window.netflix && window.netflix.appContext) {
                var api = window.netflix.appContext.state.playerApp.getAPI().videoPlayer;
                var sessionIds = api.getVideoPlayerSessionIds();
                if (sessionIds && sessionIds.length > 0) {
                    return api.getVideoPlayerBySessionId(sessionIds[sessionIds.length - 1]);
                }
            }
        } catch (e) {}
        return null;
    }

    function parseTimeString(str) {
        if (!str) return 0;
        var parts = str.trim().split(':').map(Number);
        if (parts.length === 2) return (parts[0] * 60) + parts[1];
        if (parts.length === 3) return (parts[0] * 3600) + (parts[1] * 60) + parts[2];
        return 0;
    }

    function findActiveMedia() {
        var videos = deepQueryAll('video');
        for (var i = 0; i < videos.length; i++) {
            var v = videos[i];
            // Auto unblock PiP attributes
            try {
                if (v.hasAttribute('disablepictureinpicture')) v.removeAttribute('disablepictureinpicture');
                var cl = v.getAttribute('controlslist') || '';
                if (cl.includes('nopictureinpicture')) v.setAttribute('controlslist', cl.replace('nopictureinpicture', '').trim());
            } catch (e) {}

            if (!v.paused || v.currentTime > 0) return v;
        }
        if (videos.length > 0) return videos[0];

        var audios = deepQueryAll('audio');
        if (audios.length > 0) return audios[0];
        return null;
    }

    function extractSubtitles(media) {
        try {
            // 1. YouTube Live Captions
            var ytCaps = document.querySelectorAll('.ytp-caption-segment, .caption-window');
            if (ytCaps && ytCaps.length > 0) {
                var ytTexts = [];
                for (var i = 0; i < ytCaps.length; i++) {
                    var t = ytCaps[i].textContent;
                    if (t && t.trim()) ytTexts.push(t.trim());
                }
                if (ytTexts.length > 0) return ytTexts.join(' ');
            }

            // 2. Netflix Subtitles
            var netflixCaps = document.querySelectorAll('.player-timedtext-text-container span, .player-timedtext');
            if (netflixCaps && netflixCaps.length > 0) {
                var nfTexts = [];
                for (var j = 0; j < netflixCaps.length; j++) {
                    var nt = netflixCaps[j].textContent;
                    if (nt && nt.trim()) nfTexts.push(nt.trim());
                }
                if (nfTexts.length > 0) return nfTexts.join(' ');
            }

            // 3. HTML5 TextTracks
            if (media && media.textTracks) {
                for (var k = 0; k < media.textTracks.length; k++) {
                    var track = media.textTracks[k];
                    if (track.mode === 'showing' && track.activeCues && track.activeCues.length > 0) {
                        var texts = [];
                        for (var c = 0; c < track.activeCues.length; c++) {
                            texts.push(track.activeCues[c].text);
                        }
                        return texts.join(' ');
                    }
                }
            }

            // 4. Generic Subtitles Container
            var genericCaps = document.querySelectorAll('[class*=""caption-segment""], [class*=""timedtext""], [class*=""subtitles""]');
            if (genericCaps && genericCaps.length > 0) {
                var gText = genericCaps[0].textContent;
                if (gText && gText.trim()) return gText.trim();
            }
        } catch (e) {}
        return '';
    }

    function notifyMediaState() {
        unblockPictureInPicture();

        var yt = getYouTubePlayer();
        var nf = getNetflixPlayer();
        var media = findActiveMedia();

        var hasMedia = false;
        var isPlaying = false;
        var currentTime = 0;
        var duration = 0;
        var volume = 1.0;
        var isMuted = false;
        var supportsPip = false;
        var currentCaption = '';

        if (yt) {
            hasMedia = true;
            isPlaying = yt.getPlayerState ? yt.getPlayerState() === 1 : false;
            currentTime = yt.getCurrentTime ? yt.getCurrentTime() : 0;
            duration = yt.getDuration ? yt.getDuration() : 0;
            volume = yt.getVolume ? yt.getVolume() / 100 : 1.0;
            isMuted = yt.isMuted ? yt.isMuted() : false;
            supportsPip = !!(document.pictureInPictureEnabled && media && media.requestPictureInPicture);
            currentCaption = extractSubtitles(media);
        } else if (nf) {
            hasMedia = true;
            isPlaying = nf.isPlaying ? nf.isPlaying() : false;
            currentTime = nf.getCurrentTime ? nf.getCurrentTime() / 1000 : 0;
            duration = nf.getDuration ? nf.getDuration() / 1000 : 0;
            volume = nf.getVolume ? nf.getVolume() : 1.0;
            isMuted = nf.isMuted ? nf.isMuted() : false;
            supportsPip = !!(document.pictureInPictureEnabled && media && media.requestPictureInPicture);
            currentCaption = extractSubtitles(media);
        } else if (media) {
            hasMedia = true;
            isPlaying = !media.paused;
            currentTime = media.currentTime || 0;
            duration = isFinite(media.duration) ? media.duration : 0;
            volume = media.volume ?? 1.0;
            isMuted = media.muted ?? false;
            supportsPip = !!(document.pictureInPictureEnabled && media.requestPictureInPicture);
            currentCaption = extractSubtitles(media);
        } else {
            // Check Spotify Web Player DOM
            var spotifyPos = document.querySelector('[data-testid=""playback-position""]');
            var spotifyDur = document.querySelector('[data-testid=""playback-duration""]');
            var spotifyPlayBtn = document.querySelector('[data-testid=""control-button-playpause""]');
            if (spotifyPos && spotifyDur) {
                hasMedia = true;
                currentTime = parseTimeString(spotifyPos.textContent);
                duration = parseTimeString(spotifyDur.textContent);
                isPlaying = spotifyPlayBtn ? spotifyPlayBtn.getAttribute('aria-label') === 'Pause' : false;
            }
        }

        var info = {
            type: 'FLOATVIEW_MEDIA_STATE',
            hasMedia: hasMedia,
            isPlaying: isPlaying,
            currentTime: currentTime,
            duration: duration,
            title: document.title || '',
            volume: volume,
            isMuted: isMuted,
            supportsNativePip: supportsPip,
            currentCaption: currentCaption
        };

        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage(JSON.stringify(info));
        }
    }

    setInterval(notifyMediaState, 1000);
    window.addEventListener('play', notifyMediaState, true);
    window.addEventListener('pause', notifyMediaState, true);
    window.addEventListener('timeupdate', notifyMediaState, true);
    document.addEventListener('DOMContentLoaded', notifyMediaState);
})();
";

    public static async Task TogglePlayPauseAsync(CoreWebView2 webView)
    {
        var script = @"
(function() {
    var yt = document.getElementById('movie_player');
    if (yt && typeof yt.getPlayerState === 'function') {
        if (yt.getPlayerState() === 1) yt.pauseVideo(); else yt.playVideo();
        return;
    }
    if (window.netflix && window.netflix.appContext) {
        try {
            var api = window.netflix.appContext.state.playerApp.getAPI().videoPlayer;
            var sessionIds = api.getVideoPlayerSessionIds();
            if (sessionIds && sessionIds.length > 0) {
                var p = api.getVideoPlayerBySessionId(sessionIds[sessionIds.length - 1]);
                if (p.isPlaying()) p.pause(); else p.play();
                return;
            }
        } catch(e) {}
    }
    var spotifyBtn = document.querySelector('[data-testid=""control-button-playpause""]');
    if (spotifyBtn) { spotifyBtn.click(); return; }

    var v = document.querySelector('video') || document.querySelector('audio');
    if (v) {
        if (v.paused) v.play(); else v.pause();
    }
})();";
        await webView.ExecuteScriptAsync(script);
    }

    public static async Task PlayAsync(CoreWebView2 webView)
    {
        var script = @"
(function() {
    var yt = document.getElementById('movie_player');
    if (yt && typeof yt.playVideo === 'function') { yt.playVideo(); return; }
    var v = document.querySelector('video') || document.querySelector('audio');
    if (v) v.play();
})();";
        await webView.ExecuteScriptAsync(script);
    }

    public static async Task PauseAsync(CoreWebView2 webView)
    {
        var script = @"
(function() {
    var yt = document.getElementById('movie_player');
    if (yt && typeof yt.pauseVideo === 'function') { yt.pauseVideo(); return; }
    var v = document.querySelector('video') || document.querySelector('audio');
    if (v) v.pause();
})();";
        await webView.ExecuteScriptAsync(script);
    }

    public static async Task SetVolumeAsync(CoreWebView2 webView, double volume)
    {
        volume = Math.Clamp(volume, 0.0, 1.0);
        string volStr = volume.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var script = $@"
(function() {{
    var yt = document.getElementById('movie_player');
    if (yt && typeof yt.setVolume === 'function') {{ yt.setVolume({volStr} * 100); yt.unMute(); return; }}
    var v = document.querySelector('video') || document.querySelector('audio');
    if (v) {{ v.volume = {volStr}; v.muted = false; }}
}})();";
        await webView.ExecuteScriptAsync(script);
    }

    public static async Task ToggleMuteAsync(CoreWebView2 webView)
    {
        var script = @"
(function() {
    var yt = document.getElementById('movie_player');
    if (yt && typeof yt.isMuted === 'function') {
        if (yt.isMuted()) yt.unMute(); else yt.mute();
        return;
    }
    var v = document.querySelector('video') || document.querySelector('audio');
    if (v) v.muted = !v.muted;
})();";
        await webView.ExecuteScriptAsync(script);
    }

    public static async Task SeekRelativeAsync(CoreWebView2 webView, double seconds)
    {
        string secStr = seconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var script = $@"
(function() {{
    var yt = document.getElementById('movie_player');
    if (yt && typeof yt.getCurrentTime === 'function' && typeof yt.seekTo === 'function') {{
        var target = Math.max(0, yt.getCurrentTime() + {secStr});
        yt.seekTo(target, true);
        return;
    }}
    if (window.netflix && window.netflix.appContext) {{
        try {{
            var api = window.netflix.appContext.state.playerApp.getAPI().videoPlayer;
            var sessionIds = api.getVideoPlayerSessionIds();
            if (sessionIds && sessionIds.length > 0) {{
                var p = api.getVideoPlayerBySessionId(sessionIds[sessionIds.length - 1]);
                var cur = p.getCurrentTime();
                p.seek(Math.max(0, cur + ({secStr} * 1000)));
                return;
            }}
        }} catch(e) {{}}
    }}
    var v = document.querySelector('video') || document.querySelector('audio');
    if (v) {{ v.currentTime = Math.max(0, v.currentTime + {secStr}); }}
}})();";
        await webView.ExecuteScriptAsync(script);
    }

    public static async Task<bool> RequestNativePipAsync(CoreWebView2 webView)
    {
        var script = @"
(async function() {
    try {
        if (!document.pictureInPictureEnabled) return false;

        // Unblock attributes on all videos
        var videos = document.querySelectorAll('video');
        for (var i = 0; i < videos.length; i++) {
            var v = videos[i];
            if (v.hasAttribute('disablepictureinpicture')) v.removeAttribute('disablepictureinpicture');
            var cl = v.getAttribute('controlslist') || '';
            if (cl.includes('nopictureinpicture')) v.setAttribute('controlslist', cl.replace('nopictureinpicture', '').trim());
        }

        if (document.pictureInPictureElement) {
            await document.exitPictureInPicture();
            return false;
        }

        // Find playing or first video
        var video = null;
        for (var j = 0; j < videos.length; j++) {
            if (!videos[j].paused || videos[j].currentTime > 0) { video = videos[j]; break; }
        }
        if (!video && videos.length > 0) video = videos[0];
        if (!video) return false;

        await video.requestPictureInPicture();
        return true;
    } catch(e) {
        return false;
    }
})();";
        var result = await webView.ExecuteScriptAsync(script);
        return result != null && result.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
    }
}
