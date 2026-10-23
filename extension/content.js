// FloatView Companion Content Script - Real-time Media Detection & Control

(function () {
    let lastReportedTime = 0;
    let lastReportTimeMs = 0;
    let isCurrentlyPlaying = false;

    function deepQueryAll(selector, root) {
        root = root || document;
        const results = [];
        try {
            if (root.querySelectorAll) {
                const direct = root.querySelectorAll(selector);
                for (let i = 0; i < direct.length; i++) results.push(direct[i]);
            }
            const walker = document.createTreeWalker(root, NodeFilter.SHOW_ELEMENT, null, false);
            while (walker.nextNode()) {
                const el = walker.currentNode;
                if (el.shadowRoot) {
                    const shadowMatches = deepQueryAll(selector, el.shadowRoot);
                    for (let j = 0; j < shadowMatches.length; j++) results.push(shadowMatches[j]);
                }
            }
        } catch (e) { }
        return results;
    }

    function getYouTubePlayer() {
        try {
            const el = document.getElementById('movie_player');
            if (el && typeof el.getCurrentTime === 'function') return el;
        } catch (e) { }
        return null;
    }

    function getActiveMediaElement() {
        const videos = deepQueryAll('video');
        for (let i = 0; i < videos.length; i++) {
            const v = videos[i];
            if (!v.paused || v.currentTime > 0) return v;
        }
        const audios = deepQueryAll('audio');
        for (let i = 0; i < audios.length; i++) {
            const a = audios[i];
            if (!a.paused || a.currentTime > 0) return a;
        }
        return null;
    }

    function extractCleanTitle() {
        let title = document.title || '';
        // If YouTube, title is often "(12) Video Name - YouTube"
        title = title.replace(/^\(\d+\)\s*/, ''); // remove notification badge like (1)
        return title.trim();
    }

    function sendMediaState(type, mediaEl) {
        try {
            const yt = getYouTubePlayer();
            let isPlaying = false;
            let currentTime = 0;
            let duration = 0;
            let isMuted = false;

            if (yt) {
                const state = yt.getPlayerState ? yt.getPlayerState() : -1;
                isPlaying = (state === 1); // 1 = playing
                currentTime = yt.getCurrentTime ? yt.getCurrentTime() : 0;
                duration = yt.getDuration ? yt.getDuration() : 0;
                isMuted = yt.isMuted ? yt.isMuted() : false;
            } else if (mediaEl) {
                isPlaying = !mediaEl.paused && !mediaEl.ended;
                currentTime = mediaEl.currentTime || 0;
                duration = isFinite(mediaEl.duration) ? mediaEl.duration : 0;
                isMuted = mediaEl.muted || false;
            } else {
                return;
            }

            isCurrentlyPlaying = isPlaying;

            const payload = {
                type: type,
                url: window.location.href,
                title: extractCleanTitle(),
                domain: window.location.hostname,
                currentTime: currentTime,
                duration: duration,
                isPlaying: isPlaying,
                isMuted: isMuted
            };

            chrome.runtime.sendMessage({
                action: 'FLOATVIEW_MEDIA_EVENT',
                payload: payload
            });
        } catch (e) {
            // Extension context may be invalidated during reload
        }
    }

    function attachMediaListeners(media) {
        if (!media || media._floatviewAttached) return;
        media._floatviewAttached = true;

        media.addEventListener('play', () => sendMediaState('MEDIA_PLAYING', media));
        media.addEventListener('playing', () => sendMediaState('MEDIA_PLAYING', media));
        media.addEventListener('pause', () => sendMediaState('MEDIA_PAUSED', media));
        media.addEventListener('ended', () => sendMediaState('MEDIA_ENDED', media));

        media.addEventListener('timeupdate', () => {
            const now = Date.now();
            // Throttle time updates to once every 2 seconds when playing
            if (isCurrentlyPlaying && (now - lastReportTimeMs > 2000)) {
                lastReportTimeMs = now;
                sendMediaState('MEDIA_TIMEUPDATE', media);
            }
        });
    }

    function scanAndAttach() {
        const mediaElements = [...deepQueryAll('video'), ...deepQueryAll('audio')];
        for (const el of mediaElements) {
            attachMediaListeners(el);
            if (!el.paused && el.currentTime > 0) {
                sendMediaState('MEDIA_PLAYING', el);
            }
        }
    }

    // Observer for dynamically added videos/audio
    const observer = new MutationObserver(() => scanAndAttach());
    observer.observe(document.documentElement, { childList: true, subtree: true });

    // Initial scan
    scanAndAttach();

    // Listen for safe handoff commands from FloatView
    chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
        if (request && request.action === 'FLOATVIEW_PAUSE') {
            const yt = getYouTubePlayer();
            if (yt && typeof yt.pauseVideo === 'function') {
                yt.pauseVideo();
                sendResponse({ status: 'paused', target: 'youtube' });
                return true;
            }

            const mediaEl = getActiveMediaElement();
            if (mediaEl) {
                mediaEl.pause();
                sendResponse({ status: 'paused', target: 'mediaElement' });
                return true;
            }

            sendResponse({ status: 'no_active_media' });
        }
        return true;
    });
})();
