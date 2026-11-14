/* Rotation Timer sound and screen-awake helpers, shared by the main site
   (index.html) and Table Tools Lite (lite/index.html). Plain JavaScript
   that older Safari understands.

   dstBeepUnlock(): call from a tap (Start, the sound button). iPhones and
   iPads only allow sound that a tap has started, so this wakes the audio
   up ready for the beep later.
   dstBeep(n): n short beeps. Silent when the device's ringer switch is
   on silent, or the screen is locked.
   dstKeepAwake(on): asks the screen to stay on while the timer runs
   (iOS 16.4 and newer, current Android and desktop browsers; ignored where
   not supported). */
(function () {
    var ctx = null;

    function audio() {
        if (!ctx) {
            var C = window.AudioContext || window.webkitAudioContext;
            if (!C) return null;
            try { ctx = new C(); } catch (e) { return null; }
        }
        if (ctx.state === 'suspended' && ctx.resume) {
            try { ctx.resume(); } catch (e) { }
        }
        return ctx;
    }

    window.dstBeepUnlock = function () {
        var c = audio();
        if (!c) return;
        try {
            // A silent sound started by the tap unlocks audio on iOS
            var src = c.createBufferSource();
            src.buffer = c.createBuffer(1, 1, 22050);
            src.connect(c.destination);
            if (src.start) src.start(0); else src.noteOn(0);
        } catch (e) { }
    };

    window.dstBeep = function (n) {
        var c = audio();
        if (!c) return;
        n = n || 3;
        try {
            var t = c.currentTime + 0.05;
            for (var i = 0; i < n; i++) {
                var o = c.createOscillator(), g = c.createGain();
                o.type = 'square';
                o.frequency.value = 880;
                g.gain.setValueAtTime(0.0001, t);
                g.gain.exponentialRampToValueAtTime(0.25, t + 0.02);
                g.gain.setValueAtTime(0.25, t + 0.24);
                g.gain.exponentialRampToValueAtTime(0.0001, t + 0.32);
                o.connect(g);
                g.connect(c.destination);
                o.start(t);
                o.stop(t + 0.35);
                t += 0.5;
            }
        } catch (e) { }
    };

    var lock = null, wantAwake = false;

    function requestAwake() {
        if (!wantAwake || lock || !navigator.wakeLock || !navigator.wakeLock.request) return;
        navigator.wakeLock.request('screen').then(function (l) {
            lock = l;
            lock.addEventListener('release', function () { lock = null; });
        }).catch(function () { });
    }

    window.dstKeepAwake = function (on) {
        wantAwake = !!on;
        if (wantAwake) requestAwake();
        else if (lock) { try { lock.release(); } catch (e) { } lock = null; }
    };

    // The browser drops the screen lock when the page is hidden; take it
    // back when the page shows again while the timer still runs
    document.addEventListener('visibilitychange', function () {
        if (document.visibilityState === 'visible') requestAwake();
    });
})();
