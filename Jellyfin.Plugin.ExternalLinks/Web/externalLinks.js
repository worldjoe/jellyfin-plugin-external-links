// Sends Play on linked library items to their external URL (jellyfin-web only).
// Two layers: a click interceptor, and a fetch hook on playback requests as a fallback.
(function () {
    'use strict';
    if (window.__externalLinksLoaded) { return; }
    window.__externalLinksLoaded = true;

    var links = {};
    var loadedAt = 0;
    var ready = null;
    var TTL = 60000;
    var PLAY_SEL = '.btnPlay, .btnResume, [data-action="play"], [data-action="resume"], .cardOverlayFab-primary';
    var origFetch = window.fetch.bind(window);

    function log() {
        var a = Array.prototype.slice.call(arguments);
        a.unshift('[ExternalLinks]');
        console.log.apply(console, a);
    }

    function norm(id) { return String(id).replace(/-/g, '').toLowerCase(); }

    function refresh(force) {
        var c = window.ApiClient;
        if (!c || !c.accessToken()) { return ready; }
        if (!force && ready && Date.now() - loadedAt < TTL) { return ready; }
        loadedAt = Date.now();
        ready = origFetch(c.getUrl('ExternalLinks/Links'), {
            headers: { 'Authorization': 'MediaBrowser Token="' + c.accessToken() + '"' }
        }).then(function (r) { return r.ok ? r.json() : {}; })
          .then(function (m) {
              var map = {};
              Object.keys(m || {}).forEach(function (k) { map[norm(k)] = m[k]; });
              links = map;
              log('loaded', Object.keys(links).length, 'link(s)');
          })
          .catch(function (e) { loadedAt = 0; log('load failed', e); });
        return ready;
    }

    function go(url) {
        log('opening', url);
        window.location.assign(url);
    }

    function itemIdFor(el) {
        var holder = el.closest('[data-id]');
        if (holder) { return norm(holder.getAttribute('data-id')); }
        var m = /[?&]id=([0-9a-fA-F-]{32,36})/.exec(window.location.hash);
        return m ? norm(m[1]) : null;
    }

    document.addEventListener('click', function (e) {
        var btn = e.target.closest && e.target.closest(PLAY_SEL);
        if (!btn) { return; }
        var id = itemIdFor(btn);
        var url = id && links[id];
        if (!url) { return; }
        e.preventDefault();
        e.stopImmediatePropagation();
        go(url);
    }, true);

    // Fallback: playback always asks for PlaybackInfo (or a stream) first.
    var ITEM_RE = /\/(?:Items|Videos)\/([0-9a-fA-F-]{32,36})\/(?:PlaybackInfo|stream|master\.m3u8|main\.m3u8|live\.m3u8)/i;
    window.fetch = function (input) {
        var u = typeof input === 'string' ? input : (input && input.url) || '';
        var m = ITEM_RE.exec(u);
        if (!m) { return origFetch.apply(window, arguments); }
        var args = arguments;
        var id = norm(m[1]);
        return Promise.resolve(refresh(false)).then(function () {
            var url = links[id];
            if (!url) { return origFetch.apply(window, args); }
            go(url);
            return new Promise(function () { });
        });
    };

    function boot() {
        if (window.ApiClient && window.ApiClient.accessToken()) {
            refresh(true);
        } else {
            setTimeout(boot, 1000);
        }
    }

    boot();
    window.addEventListener('hashchange', function () { refresh(false); });
    setInterval(function () { refresh(true); }, TTL);
    log('script loaded');
})();
