/* Table Tools Lite: Limit Sign, Progressive Touchscreen, Player Tracking,
   Shuffle Timer, Pai Gow Number and Pay Tables, for older iPads.
   Plain JavaScript (no build step). Each tool copies its main-site page:
     Pages/ToolLimitSign.razor, Pages/ProgressiveTrainer.razor (Touchscreen
     only), Pages/ToolPlayerTracking.razor, Pages/ToolShuffleTimer.razor,
     Pages/ToolPaiGowNumber.razor, Pages/ToolPayTables.razor,
     Shared/HouseWayTable.razor; progressive pay tables from
     Games/ProgressiveTrainer/ProgressiveGames.cs.
   Change one, change the other. */
(function () {
    'use strict';

    var app = document.getElementById('lite-app');

    // ---- Small helpers -----------------------------------------------------

    function esc(s) {
        return String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }

    function load(key) {
        try { return window.localStorage.getItem(key); } catch (e) { return null; }
    }

    function save(key, value) {
        try { window.localStorage.setItem(key, value); } catch (e) { }
    }

    function rnd(lo, hi) { return lo + Math.floor(Math.random() * (hi - lo + 1)); }

    function strip(title, text, extra) {
        return '<div class="tl-strip">' +
            '<button type="button" class="tl-exit" data-act="home">&lsaquo; Tools</button>' +
            '<b>' + esc(title) + '</b>' +
            '<span>' + esc(text || '') + '</span>' + (extra || '') +
            '</div>';
    }

    // ---- Views ---------------------------------------------------------------

    var view = 'home';
    var timers = [];

    function stopTimers() {
        timers.forEach(function (t) { clearTimeout(t); clearInterval(t); });
        timers = [];
    }

    function go(v) {
        stopTimers();
        view = v;
        if (v === 'prog') prog.game = -1;
        if (v === 'pay') pay.tab = 0;
        if (v.indexOf('table:') === 0) {
            var ti = parseInt(v.slice(6), 10);
            prog.game = TABLES[ti].game;
            progClear();
            pay.tab = 0;
            if (timer.state !== 'done') timer.state = 'ready';
        }
        if (v === 'timer' && timer.state !== 'done') timer.state = 'ready';
        render();
        window.scrollTo(0, 0);
    }

    function render() {
        var html;
        switch (view) {
            case 'limit': html = limitView(); break;
            case 'prog': html = progView(); break;
            case 'track': html = trackView(); break;
            case 'timer': html = timerView(); break;
            case 'pgn': html = pgnView(); break;
            case 'pay': html = payView(); break;
            default: html = view.indexOf('table:') === 0 ? tableView(parseInt(view.slice(6), 10)) : homeView();
        }
        // Every tool screen carries the training disclaimer along the bottom
        // (Home has it in full at the foot of the page)
        if (view !== 'home') html += '<p class="lt-disc">Training simulation only &middot; Not a real gaming system &middot; Not affiliated with or endorsed by any game or equipment manufacturer</p>';
        app.innerHTML = html;
    }

    // ---- Home ----------------------------------------------------------------

    var TOOLS = [
        { v: 'limit', name: 'Limit Sign', text: 'Tap MINIMUM or MAXIMUM to change it', ic: '$' },
        { v: 'prog', name: 'Progressive Touchscreen', text: 'Sensors, PLAY, PAYOUT, END GAME', ic: '$5', gold: true },
        { v: 'track', name: 'Player Tracking', text: 'Log in, seat a guest, enter the buy-in', ic: 'IN' },
        { v: 'timer', name: 'Shuffle Timer', text: '6-deck shuffle, 90-second ideal', ic: '90' },
        { v: 'pgn', name: 'Pai Gow Number', text: 'A number 1-7, or 3 dice', ic: '7' },
        { v: 'pay', name: 'Pay Tables', text: 'Every game, a tab per wager', ic: '%' }
    ];

    function homeView() {
        var h = '<div class="lt-home">' +
            '<h1>Table Tools <span>Lite</span></h1>' +
            '<p class="lt-sub">For the training table &middot; Dealer School Trainer</p>' +
            '<h2 class="lt-h2">Table Scenes</h2><div class="lt-tables">';
        TABLES.forEach(function (t, i) {
            h += '<button type="button" class="lt-table" data-act="go" data-v="table:' + i + '"><b>' + esc(t.name) + '</b><span>' + esc(t.text) + '</span></button>';
        });
        h += '</div><h2 class="lt-h2">Tools</h2><div class="lt-grid">';
        TOOLS.forEach(function (t) {
            h += '<button type="button" class="lt-tile' + (t.gold ? ' lt-gold' : '') + '" data-act="go" data-v="' + t.v + '">' +
                '<span class="lt-ic">' + esc(t.ic) + '</span>' +
                '<span class="lt-tx"><b>' + esc(t.name) + '</b><span>' + esc(t.text) + '</span></span></button>';
        });
        h += '</div>' +
            '<p class="lt-foot">Training simulation only. Not affiliated with or endorsed by any game or equipment manufacturer. Game names are trademarks of their owners.</p>' +
            '</div>';
        return h;
    }

    // ---- Table limit lock ----------------------------------------------------
    // Locked: tapping MINIMUM / MAXIMUM does nothing, so a bump at the table
    // can't change the limit. Unlocked: tap to cycle. Locked to start;
    // remembered per sign.
    function isLocked(key) { return load('dst-lock-' + key) !== '0'; }

    function lockBtn(key) {
        var locked = isLocked(key);
        return '<button type="button" class="lt-lock' + (locked ? ' lt-locked' : '') + '" data-act="lock" data-key="' + key + '" aria-pressed="' + locked + '">' +
            (locked ? '&#x1F512;&#xFE0E; Locked' : '&#x1F513;&#xFE0E; Unlocked') + '</button>';
    }

    // ---- Limit Sign (ToolLimitSign.razor) -----------------------------------

    var MINS = [5, 10, 15, 25, 50], MAXES = [100, 300, 500];
    var limit = { min: 0, max: 0 };
    (function () {
        var p = (load('dst-limit-sign') || '').split(',');
        var a = parseInt(p[0], 10), b = parseInt(p[1], 10);
        if (p.length === 2 && a >= 0 && a < MINS.length && b >= 0 && b < MAXES.length) { limit.min = a; limit.max = b; }
    })();

    function dots(n, on) {
        var h = '<span class="tl-dots" aria-hidden="true">';
        for (var i = 0; i < n; i++) h += '<i class="' + (i === on ? 'tl-on' : '') + '"></i>';
        return h + '</span>';
    }

    function limitView() {
        var locked = isLocked('sign'), tap = locked ? 'locked' : 'tap to change';
        return '<div class="tl-stage tl-full lt-stage">' +
            strip('Table Limit Sign', locked ? 'Locked: unlock to change the limits' : 'Tap a number to change it', lockBtn('sign')) +
            '<div class="tl-sign' + (locked ? ' lt-sign-locked' : '') + '">' +
            '<button type="button" class="tl-sign-half" data-act="min"><span class="tl-k">MINIMUM</span><span class="tl-v">$' + MINS[limit.min] + '</span>' + dots(MINS.length, limit.min) + '<span class="tl-tap">' + tap + '</span></button>' +
            '<span class="tl-sign-rule" aria-hidden="true"></span>' +
            '<button type="button" class="tl-sign-half" data-act="max"><span class="tl-k">MAXIMUM</span><span class="tl-v">$' + MAXES[limit.max] + '</span>' + dots(MAXES.length, limit.max) + '<span class="tl-tap">' + tap + '</span></button>' +
            '</div></div>';
    }

    // ---- Progressive Touchscreen (ProgressiveTrainer.razor, Touchscreen only)

    function W(header, chipTop, chipSub, tag, cls, rows) {
        return { header: header, chipTop: chipTop, chipSub: chipSub, tag: tag, cls: cls, rows: rows };
    }

    var PROG = [
        {
            name: 'Blackjack', wagers: [
                W('21+3 $5', '$5', 'Jackpot', '$5', 'pr-h5', [['3 of a Kind Aces (Suited)', '100%'], ['3 of a Kind (Suited)', '$500'], ['Straight Flush', '$100'], ['3 of a Kind (Offsuit)', '$50'], ['Straight', '$10']]),
                W('21+3 $1', '$1', 'Jackpot', '$1', 'pr-h1', [['3 of a Kind Aces (Suited)', '100%'], ['3 of a Kind (Suited)', '$100'], ['Straight Flush', '$20'], ['3 of a Kind (Offsuit)', '$10'], ['Straight', '$2']])
            ]
        },
        {
            name: 'Three Card Prime', wagers: [
                W('3CP $5', '$5', 'Jackpot', '$5', 'pr-h5', [['Mini-Royal (Diamonds)', '100%'], ['Mini-Royal (Other)', '$1,000'], ['Straight Flush', '$100'], ['Three of a Kind', '$50'], ['Straight', '$10']]),
                W('3CP $1', '$1', 'Jackpot', '$1', 'pr-h1', [['Mini-Royal (Spades)', '100%'], ['Mini-Royal (Other)', '$200'], ['Straight Flush', '$20'], ['Three of a Kind', '$10'], ['Straight', '$2']])
            ]
        },
        {
            name: 'Pai Gow', wagers: [
                W('ET $1', 'ET', 'Emperor', 'ET $1', 'pr-h5', [['7-Card SF (no Joker)', '100%'], ['Royal + A-K Suited', '50%'], ['7-Card Straight Flush', '25%'], ['Five Aces', '$1,000'], ['Royal Flush', '$175'], ['Straight Flush', '$150'], ['Four of a Kind', '$75'], ['Full House', '$5']]),
                W('INS $1', 'INS', 'Insurance', 'Ins $1', 'pr-h1', [['9-High Same Color', '100%'], ['9-High Pai Gow', '$250'], ['10-High Pai Gow', '$125'], ['Jack-High Pai Gow', '$30'], ['Queen-High Pai Gow', '$7']])
            ]
        },
        {
            name: "Player's Edge-21", wagers: [
                W('PE21 $5', '$5', 'Jackpot', '$5', 'pr-h5', [['Four of a Kind (Same Color)', '100%'], ['Four of a Kind', '$1,000'], ['Three of a Kind (Same Color)', '$100'], ['Three of a Kind', '$50'], ['Pair (Suited)', '$10']]),
                W('PE21 $1', '$1', 'Jackpot', '$1', 'pr-h1', [['Four of a Kind (Same Color)', '100%'], ['Four of a Kind', '$200'], ['Three of a Kind (Same Color)', '$20'], ['Three of a Kind', '$10'], ['Pair (Suited)', '$2']])
            ]
        },
        {
            name: "Heads-Up Hold'em", wagers: [
                W('HUH $5', '$5', 'Jackpot', '$5', 'pr-h5', [['Royal Flush', '100%'], ['Community Royal', '$1,000'], ['Straight Flush', '$400'], ['Four-of-a-Kind', '$200'], ['Full House', '$20']]),
                W('HUH $1', '$1', 'Jackpot', '$1', 'pr-h1', [['Royal Flush', '100%'], ['Community Royal', '$200'], ['Straight Flush', '$80'], ['Four-of-a-Kind', '$40'], ['Full House', '$4']])
            ]
        }
    ];

    var prog = { game: -1, screen: 'lock', on: null, paid: null, w: -1, r: -1, p: -1 };

    function progClear() {
        prog.on = [[], []];
        prog.paid = [[], []];
        prog.screen = 'lock';
        prog.w = prog.r = prog.p = -1;
    }
    progClear();

    function owed(w) {
        for (var s = 1; s <= 6; s++) if (prog.on[w][s] && !prog.paid[w][s]) return true;
        return false;
    }

    // Lit $5 = red, lit $1 = blue (Pai Gow's ET and INS are both $1: blue)
    function chipColor(wg) { return wg.tag.indexOf('$5') >= 0 ? 'pr-c5' : 'pr-c1'; }

    function isMetered(pay) { return pay.indexOf('%') >= 0; }

    function progInstruction() {
        switch (prog.screen) {
            case 'lock': return 'Tap a chip to place or remove that wager. Check they match the table, then tap PLAY.';
            case 'tables': return 'Deal the game. PAYOUT for a winner, END GAME when the round is done.';
            case 'wager': return 'Tap the wager to pay, then NEXT.';
            case 'rank': return "Tap the hand's rank, then NEXT.";
            case 'pos': return 'Tap the position to pay, then NEXT.';
            default:
                return isMetered(PROG[prog.game].wagers[prog.w].rows[prog.r][1])
                    ? 'Metered pay: a supervisor is required. Then CONFIRM.'
                    : 'Pay the player, then CONFIRM.';
        }
    }

    function posNums() {
        var h = '<div class="pr-posnums" aria-hidden="true">';
        for (var s = 1; s <= 6; s++) h += '<span>' + s + '</span>';
        return h + '</div>';
    }

    function progView() {
        if (prog.game < 0) {
            var h = '<div class="tl-stage tl-full lt-stage">' + strip('Progressive Touchscreen', 'Pick the game') + '<div class="lt-pick">';
            PROG.forEach(function (g, i) {
                h += '<button type="button" class="lt-pick-btn" data-act="pgame" data-i="' + i + '"><b>' + esc(g.name) + '</b><span>' +
                    esc(g.wagers[0].header) + ' &middot; ' + esc(g.wagers[1].header) + '</span></button>';
            });
            return h + '</div></div>';
        }

        var def = PROG[prog.game];
        return '<div class="tl-stage tl-full lt-stage lt-prog">' +
            strip(def.name + ' · Touchscreen', progInstruction(), '<button type="button" class="lt-strip-btn" data-act="pgames">Game</button>') +
            progTouchscreen() + '</div>';
    }

    // The touchscreen itself (also used by the Table Scenes)
    function progTouchscreen() {
        var def = PROG[prog.game], body = '', s, w;
        var back = (prog.screen === 'wager' || prog.screen === 'rank' || prog.screen === 'pos' || prog.screen === 'confirm');

        switch (prog.screen) {
            case 'lock':
                body += posNums();
                for (w = 0; w < 2; w++) {
                    body += '<div class="pr-grid">';
                    for (s = 1; s <= 6; s++) {
                        body += '<button type="button" class="pr-chip ' + (prog.on[w][s] ? 'pr-lit ' : '') + chipColor(def.wagers[w]) +
                            (def.wagers[w].chipTop.length > 2 ? ' pr-chip-long' : '') + '" data-act="chip" data-w="' + w + '" data-s="' + s + '">' +
                            esc(def.wagers[w].chipTop) + '</button>';
                    }
                    body += '</div>';
                }
                body += '<div class="pr-ts-btns"><button type="button" class="pr-btn pr-play" data-act="play">PLAY</button></div>';
                break;

            case 'tables':
                body += '<div class="pr-pts">';
                def.wagers.forEach(function (wg) {
                    body += '<div class="pr-pt"><div class="pr-pt-hd ' + wg.cls + '">' + esc(wg.header) + '</div>';
                    wg.rows.forEach(function (r) { body += '<div class="pr-pt-row"><span>' + esc(r[0]) + '</span><b>' + esc(r[1]) + '</b></div>'; });
                    body += '</div>';
                });
                body += '</div><div class="pr-ts-btns">' +
                    '<button type="button" class="pr-btn pr-end" data-act="endgame">END GAME</button>' +
                    '<button type="button" class="pr-btn pr-pay" data-act="payout"' + (owed(0) || owed(1) ? '' : ' disabled') + '>PAYOUT</button></div>';
                break;

            case 'wager':
                for (w = 0; w < 2; w++) {
                    if (!owed(w)) continue;
                    var wg = def.wagers[w];
                    body += '<button type="button" class="pr-wline' + (prog.w === w ? ' pr-on' : '') + '" data-act="selw" data-w="' + w + '">' +
                        '<span class="pr-box">' + (prog.w === w ? '&#10003;' : '') + '</span>' +
                        '<span class="pr-wline-pill"><small>' + esc(wg.tag) + '<br />' + esc(wg.chipSub) + '</small><b>' + esc(wg.header) + '</b></span></button>';
                }
                body += '<div class="pr-ts-btns"><button type="button" class="pr-btn pr-next" data-act="next-rank"' + (prog.w < 0 ? ' disabled' : '') + '>NEXT</button></div>';
                break;

            case 'rank':
                var rw = def.wagers[prog.w];
                // One column, top to bottom, like the real screen. On a Table Scene a
                // long table (Pai Gow ET, 8 lines) goes in two columns read top to
                // bottom (first half down the left) so it fits the panel.
                var two = rw.rows.length > 5;
                body += '<div class="pr-rank-hd">' + esc(rw.header) + '</div><div class="pr-ranks' + (two ? ' lt-ranks-2' : '') + '"' +
                    (two ? ' style="grid-template-rows:repeat(' + Math.ceil(rw.rows.length / 2) + ',auto)"' : '') + '>';
                rw.rows.forEach(function (r, i) {
                    body += '<button type="button" class="pr-rank' + (prog.r === i ? ' pr-on' : '') + '" data-act="selr" data-i="' + i + '"><span>' + esc(r[0]) + '</span><b>' + esc(r[1]) + '</b></button>';
                });
                body += '</div><div class="pr-ts-btns"><button type="button" class="pr-btn pr-next" data-act="next-pos"' + (prog.r < 0 ? ' disabled' : '') + '>NEXT</button></div>';
                break;

            case 'pos':
                var pw = def.wagers[prog.w];
                body += posNums() + '<div class="pr-grid">';
                for (s = 1; s <= 6; s++) {
                    var lit = prog.on[prog.w][s] && !prog.paid[prog.w][s];
                    body += '<button type="button" class="pr-chip ' + (lit ? 'pr-lit ' : '') + (prog.p === s ? 'pr-sel ' : '') +
                        (pw.chipTop.length > 2 ? 'pr-chip-long' : '') + '" data-act="selp" data-s="' + s + '"' + (lit ? '' : ' disabled') + '>' + esc(pw.chipTop) + '</button>';
                }
                body += '</div><div class="pr-ts-btns"><button type="button" class="pr-btn pr-next" data-act="next-confirm"' + (prog.p < 0 ? ' disabled' : '') + '>NEXT</button></div>';
                break;

            default: // confirm: blank, like the real screen
                body += '<div class="pr-ts-btns"><button type="button" class="pr-btn pr-next" data-act="confirm">CONFIRM</button></div>';
        }

        return '<div class="pr-ts pr-sim"><div class="pr-ts-top">' +
            (back ? '<button type="button" class="pr-back" aria-label="Back" data-act="pback">&lsaquo;</button>' : '') +
            '</div>' + body + '</div>';
    }

    // ---- Player Tracking (ToolPlayerTracking.razor) -------------------------

    var track = { screen: 'table', loggedIn: false, loginFirst: false, sel: null, entry: '', note: null, buyins: [0, 0, 0, 0, 0, 0, 0] };

    function trackInstruction() {
        if (track.screen === 'pin') return 'Enter any PIN, then Ent.';
        if (track.screen === 'buyin') return 'Enter the cash buy-in, then Ent.';
        if (track.sel === 0) return 'Tap IN to log in.';
        if (track.sel !== null) return 'Tap IN to enter the buy-in.';
        if (!track.loggedIn) return track.loginFirst ? 'Log in first: tap D.' : 'Tap D. to log in.';
        for (var i = 1; i <= 6; i++) if (track.buyins[i] > 0) return 'Tap a position to add a guest, or a guest for another buy-in.';
        return 'Tap a position to add a guest.';
    }

    function keypad(money) {
        var h = '<div class="pk-kp">';
        ['1', '2', '3'].forEach(function (k) { h += '<button type="button" data-act="key" data-k="' + k + '">' + k + '</button>'; });
        h += '<button type="button" class="pk-del" data-act="del">Del</button>';
        ['4', '5', '6', '7', '8', '9'].forEach(function (k) { h += '<button type="button" data-act="key" data-k="' + k + '">' + k + '</button>'; });
        h += '<button type="button" class="pk-ent" data-act="ent">Ent</button>';
        h += '<button type="button" data-act="key" data-k="00">00</button><button type="button" data-act="key" data-k="0">0</button>';
        h += money ? '<button type="button" data-act="key" data-k=".">.</button>' : '<span></span>';
        return h + '</div>';
    }

    function trackView() {
        var scr = '';
        if (track.screen === 'pin') {
            scr = '<button type="button" class="pk-back" aria-label="Back" data-act="tback">&#x21A9;</button>' +
                '<div class="pk-lbl pk-lbl-pin">Dealer Pin:</div>' +
                '<div class="pk-disp">' + esc(track.entry || '0') + '</div>' + keypad(false);
        } else if (track.screen === 'buyin') {
            scr = '<button type="button" class="pk-back" aria-label="Back" data-act="tback">&#x21A9;</button>' +
                '<div class="pk-lbl pk-lbl-buy">Buyin Amt:</div>' +
                '<div class="pk-lbl pk-lbl-player">Player ' + track.sel + '</div>' +
                '<div class="pk-cash">CASH</div>' +
                '<div class="pk-disp">$' + esc(track.entry || '0') + '</div>' + keypad(true);
        } else {
            scr = '<div class="pk-title">15-300 Game Name Here</div>';
            for (var s = 1; s <= 6; s++) {
                scr += '<button type="button" class="pk-pos pk-p' + s + (track.sel === s ? ' pk-sel' : '') + '" data-act="seat" data-s="' + s + '">' +
                    '<span class="pk-n">' + s + '.</span>' + (track.buyins[s] > 0 ? '<span class="pk-g">Guest</span>' : '') + '</button>';
            }
            scr += '<button type="button" class="pk-pos pk-pd' + (track.sel === 0 ? ' pk-sel' : '') + '" data-act="dealer">' +
                '<span class="pk-n">D.</span>' + (track.loggedIn ? '<span class="pk-g">Dealer</span>' : '') + '</button>';
            scr += '<div class="pk-btns">' + (track.sel !== null ? '<button type="button" class="pk-in" data-act="in">IN</button>' : '<span></span>') +
                '<span></span><span></span><span></span><span></span></div>';
        }
        return '<div class="tl-stage tl-full lt-stage pk-stage lt-track">' +
            strip('Player Tracking', track.note ? '✓ ' + track.note : trackInstruction(), '<button type="button" class="pk-strip-reset" data-act="treset">Reset</button>') +
            '<div class="pk-bezel"><div class="pk-scr">' + scr + '</div></div></div>';
    }

    function trackBack() {
        track.entry = '';
        track.sel = null;
        track.screen = 'table';
    }

    function trackKey(k) {
        var money = track.screen === 'buyin';
        if (k === '.') {
            if (money && track.entry.indexOf('.') < 0) track.entry = track.entry.length === 0 ? '0.' : track.entry + '.';
            return;
        }
        var dot = track.entry.indexOf('.');
        if (dot >= 0 && track.entry.length - dot - 1 + k.length > 2) return;   // cents: 2 digits at most
        if (track.entry.replace('.', '').length + k.length > 8) return;        // 8 digits at most
        if (money && track.entry.length === 0 && k.charAt(0) === '0') return;  // no leading zeros on a dollar amount
        track.entry += k;
    }

    function money(d) {
        return d === Math.floor(d)
            ? '$' + d.toLocaleString('en-US')
            : '$' + d.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    function trackEnt() {
        if (track.screen === 'pin') {
            if (track.entry.length === 0) return;
            track.loggedIn = true;
            trackBack();
            return;
        }
        var amt = parseFloat(track.entry);
        if (!(amt > 0)) return;
        var seat = track.sel || 0;
        track.buyins[seat] = Math.round((track.buyins[seat] + amt) * 100) / 100;
        track.note = money(amt) + " cash buy-in sent to Player " + seat + "'s rating card" +
            (track.buyins[seat] !== amt ? ' (' + money(track.buyins[seat]) + ' total)' : '');
        trackBack();
    }

    // ---- Shuffle Timer (ToolShuffleTimer.razor) ------------------------------

    var IDEAL = 90;
    var timer = { state: 'ready', count: 0, elapsed: 0, start: 0, best: null };
    (function () {
        var p = (load('dst-shuffle-best') || '').split('|');
        if (p.length === 2 && p[0] === today() && !isNaN(parseFloat(p[1]))) timer.best = parseFloat(p[1]);
    })();

    function today() {
        var d = new Date(), m = d.getMonth() + 1, day = d.getDate();
        return d.getFullYear() + '-' + (m < 10 ? '0' : '') + m + '-' + (day < 10 ? '0' : '') + day;
    }

    function clock(secs) {
        var s = Math.floor(secs);
        return Math.floor(s / 60) + ':' + (s % 60 < 10 ? '0' : '') + (s % 60);
    }

    function timerView() {
        var t = timer, pct = 0, ringCls = '', sub = '90 s ideal', inner;
        if (t.state === 'countdown') { pct = (5 - t.count) * 20; ringCls = 'tl-ring-gold'; }
        else if (t.state === 'running' || t.state === 'done') {
            pct = Math.min(100, t.elapsed / IDEAL * 100);
            if (t.elapsed > IDEAL) ringCls = 'tl-ring-over';
        }
        if (t.state === 'running') sub = t.elapsed > IDEAL ? 'over 1:30' : 'of 1:30';
        if (t.state === 'done') {
            var d = Math.round(t.elapsed) - IDEAL;
            sub = d === 0 ? 'right on 90' : d < 0 ? (-d) + ' s under 90' : d + ' s over 90';
        }
        inner = t.state === 'countdown'
            ? '<span class="tl-count' + (t.count === 0 ? ' tl-go-txt' : '') + '">' + (t.count === 0 ? 'GO' : t.count) + '</span>'
            : '<span class="tl-time">' + clock(t.elapsed) + '</span><span class="tl-ring-sub">' + sub + '</span>';

        var label = t.state === 'countdown' ? 'Cancel' : t.state === 'running' ? 'Stop' : t.state === 'done' ? 'Again' : 'Start';
        var intro = t.state === 'countdown' ? 'Get ready…' : t.state === 'running' ? 'Shuffling. Tap Stop when the shuffle is done.' :
            t.state === 'done' ? 'Done.' : '6-deck shuffle. Ideal time: 90 seconds.';

        return '<div class="tl-stage tl-full lt-stage lt-light">' + strip('Shuffle Timer', intro) +
            '<div class="lt-panel">' + timerInner() + '</div></div>';
    }

    // The ring, result and button (also used by the Table Scenes)
    function timerInner() {
        var t = timer, pct = 0, ringCls = '', sub = '90 s ideal', inner;
        if (t.state === 'countdown') { pct = (5 - t.count) * 20; ringCls = 'tl-ring-gold'; }
        else if (t.state === 'running' || t.state === 'done') {
            pct = Math.min(100, t.elapsed / IDEAL * 100);
            if (t.elapsed > IDEAL) ringCls = 'tl-ring-over';
        }
        if (t.state === 'running') sub = t.elapsed > IDEAL ? 'over 1:30' : 'of 1:30';
        if (t.state === 'done') {
            var d = Math.round(t.elapsed) - IDEAL;
            sub = d === 0 ? 'right on 90' : d < 0 ? (-d) + ' s under 90' : d + ' s over 90';
        }
        inner = t.state === 'countdown'
            ? '<span class="tl-count' + (t.count === 0 ? ' tl-go-txt' : '') + '">' + (t.count === 0 ? 'GO' : t.count) + '</span>'
            : '<span class="tl-time">' + clock(t.elapsed) + '</span><span class="tl-ring-sub">' + sub + '</span>';
        var label = t.state === 'countdown' ? 'Cancel' : t.state === 'running' ? 'Stop' : t.state === 'done' ? 'Again' : 'Start';

        var h = '<div class="tl-timer">' +
            '<div class="tl-ring ' + ringCls + '" style="--pct:' + pct + '"><div class="tl-ring-in">' + inner + '</div></div>';
        if (t.state === 'done') {
            var secs = Math.round(t.elapsed);
            h += '<p class="tl-result ' + (t.elapsed <= IDEAL ? 'tl-ok' : 'tl-over') + '">' +
                (secs === IDEAL ? '✓ Right on the 90-second ideal' : secs < IDEAL ? '✓ Under the 90-second ideal' : 'Over the 90-second ideal') + '</p>';
        }
        h += '<button type="button" class="tl-big' + (t.state === 'countdown' || t.state === 'running' ? ' tl-big-red' : '') + '" data-act="timer">' + label + '</button>';
        if (t.best !== null) h += '<p class="tl-best">Best today: ' + clock(t.best) + '</p>';
        return h + '</div>';
    }

    function timerMain() {
        var t = timer;
        if (t.state === 'ready' || t.state === 'done') {
            stopTimers();
            t.elapsed = 0;
            t.state = 'countdown';
            t.count = 5;
            render();
            var tick = setInterval(function () {
                t.count--;
                if (t.count > 0) { render(); return; }
                clearInterval(tick);
                t.count = 0;                 // GO: the clock starts now
                t.start = Date.now();
                render();
                timers.push(setTimeout(function () {
                    t.state = 'running';
                    timers.push(setInterval(function () {
                        t.elapsed = (Date.now() - t.start) / 1000;
                        render();
                    }, 200));
                }, 700));
            }, 1000);
            timers.push(tick);
        } else if (t.state === 'countdown') {
            stopTimers();
            t.state = 'ready';
            t.elapsed = 0;
            render();
        } else {
            stopTimers();
            t.elapsed = (Date.now() - t.start) / 1000;
            t.state = 'done';
            if (t.best === null || t.elapsed < t.best) {
                t.best = t.elapsed;
                save('dst-shuffle-best', today() + '|' + t.elapsed);
            }
            render();
        }
    }

    // ---- Pai Gow Number (ToolPaiGowNumber.razor) -----------------------------

    var PIPS = { 1: [5], 2: [1, 9], 3: [1, 5, 9], 4: [1, 3, 7, 9], 5: [1, 3, 5, 7, 9], 6: [1, 3, 4, 6, 7, 9] };
    var pgn = { dice: false, number: 0, die: [0, 0, 0], numHist: [], diceHist: [], flash: false };

    function remember(list, v) {
        list.unshift(v);
        if (list.length > 5) list.length = 5;
    }

    function pgnView() {
        return '<div class="tl-stage tl-full lt-stage lt-light">' + strip('Pai Gow Number', 'Tap Roll for a new one') +
            '<div class="lt-panel lt-pgn">' + pgnInner() + '</div></div>';
    }

    // Mode switch, number or dice, Roll (also used by the Pai Gow Table Scene)
    function pgnInner() {
        var g = pgn, body;
        if (!g.dice) {
            body = '<div class="tl-num' + (g.flash ? ' tl-flash' : '') + '"><small>NUMBER</small><b>' + (g.number || '?') + '</b></div>';
        } else {
            body = '<div class="tl-dice' + (g.flash ? ' tl-flash' : '') + '">';
            g.die.forEach(function (d) {
                body += '<span class="tl-die">';
                for (var p = 1; p <= 9; p++) body += '<i class="' + (d > 0 && PIPS[d].indexOf(p) >= 0 ? 'tl-pip' : '') + '"></i>';
                body += '</span>';
            });
            body += '</div><p class="tl-total">Total<b>' + (g.die[0] ? g.die[0] + g.die[1] + g.die[2] : '?') + '</b></p>';
        }
        var hist = g.dice ? g.diceHist : g.numHist;
        return '<div class="tl-seg"><button type="button" class="' + (!g.dice ? 'tl-on' : '') + '" data-act="pgnmode" data-d="0">Number 1&#8209;7</button>' +
            '<button type="button" class="' + (g.dice ? 'tl-on' : '') + '" data-act="pgnmode" data-d="1">3 Dice</button></div>' +
            body +
            '<button type="button" class="tl-big" data-act="roll">Roll</button>' +
            (hist.length ? '<p class="tl-hist">Last: ' + hist.join(' · ') + '</p>' : '');
    }

    function roll() {
        var g = pgn;
        if (g.dice) {
            if (g.die[0] > 0) remember(g.diceHist, g.die[0] + g.die[1] + g.die[2]);
            g.die = [rnd(1, 6), rnd(1, 6), rnd(1, 6)];
        } else {
            if (g.number > 0) remember(g.numHist, g.number);
            g.number = rnd(1, 7);
        }
        // A short flash so a repeat of the same number still shows it rolled
        g.flash = true;
        render();
        timers.push(setTimeout(function () { g.flash = false; render(); }, 250));
    }

    // ---- Pay Tables (ToolPayTables.razor) ------------------------------------

    function T(title, cls, rows) { return { title: title, cls: cls, rows: rows }; }
    function One(rows) { return [T('', '', rows)]; }
    function Tab(name, short, note, tables, houseWay) { return { name: name, short: short, note: note, tables: tables || [], houseWay: !!houseWay }; }

    var PAY = [
        {
            name: 'Blackjack', tabs: [
                Tab('21+3', '21+3', 'The player\'s 2 cards and the dealer\'s up card.',
                    One([['Straight Flush', '30 to 1'], ['Three of a Kind', '20 to 1'], ['Straight', '10 to 1'], ['Flush', '5 to 1']])),
                Tab('Lucky Ladies', 'Lucky Ladies', 'The player\'s first 2 cards total 20.',
                    One([['Q♥ Pair + Dealer Blackjack', '1000 to 1'], ['Q♥ Pair', '125 to 1'], ['Matched 20', '19 to 1'], ['Suited 20', '9 to 1'], ['Any 20', '4 to 1']])),
                Tab('Progressive', 'Progressive', '21+3 Progressive Jackpot. A percentage pays from the meter: supervisor required.', [
                    T('$5', 'tl-h5', [['3 of a Kind Aces (Suited)', '100%'], ['3 of a Kind (Suited)', '$500'], ['Straight Flush', '$100'], ['3 of a Kind (Offsuit)', '$50'], ['Straight', '$10']]),
                    T('$1', 'tl-h1', [['3 of a Kind Aces (Suited)', '100%'], ['3 of a Kind (Suited)', '$100'], ['Straight Flush', '$20'], ['3 of a Kind (Offsuit)', '$10'], ['Straight', '$2']])])
            ]
        },
        {
            name: 'Three Card', tabs: [
                Tab('Ante Bonus', 'Ante Bonus', 'Paid on the Ante, win or lose against the dealer. Play and Ante pay 1 to 1.',
                    One([['Straight Flush', '5 to 1'], ['Three of a Kind', '4 to 1'], ['Straight', '1 to 1']])),
                Tab('Pair Bonus', 'Pair Bonus', 'The player\'s 3 cards.',
                    One([['Straight Flush', '40 to 1'], ['Three of a Kind', '30 to 1'], ['Straight', '6 to 1'], ['Flush', '3 to 1'], ['Pair', '1 to 1']])),
                Tab('Prime', 'Prime', 'Card colors: red or black.',
                    One([['All 6 cards same color', '4 to 1'], ['Player\'s 3 cards same color', '3 to 1']])),
                Tab('All 6', 'All 6', 'The best 5-card hand from the player\'s 3 and the dealer\'s 3.',
                    One([['Royal Flush', '1000 to 1'], ['Straight Flush', '200 to 1'], ['Four of a Kind', '50 to 1'], ['Full House', '25 to 1'], ['Flush', '20 to 1'], ['Straight', '10 to 1'], ['Three of a Kind', '5 to 1']])),
                Tab('Progressive', 'Prog.', 'Mini Royal Progressive: the player\'s 3 cards. A percentage pays from the meter: supervisor required.', [
                    T('$5', 'tl-h5', [['Mini-Royal (Diamonds)', '100%'], ['Mini-Royal (Other)', '$1,000'], ['Straight Flush', '$100'], ['Three of a Kind', '$50'], ['Straight', '$10']]),
                    T('$1', 'tl-h1', [['Mini-Royal (Spades)', '100%'], ['Mini-Royal (Other)', '$200'], ['Straight Flush', '$20'], ['Three of a Kind', '$10'], ['Straight', '$2']])])
            ]
        },
        {
            name: 'Pai Gow', tabs: [
                Tab('Insurance', 'Insurance', 'Pays when the player\'s 7 cards make no pair, straight or flush (a Pai Gow). The Joker counts as an Ace.',
                    One([['9-High Pai Gow', '100 to 1'], ['10-High Pai Gow', '25 to 1'], ['Jack-High Pai Gow', '15 to 1'], ['Queen-High Pai Gow', '6 to 1'], ['King-High Pai Gow', '5 to 1'], ['Ace-High Pai Gow', '3 to 1']])),
                Tab('Emperor\'s Treasure', 'Emperor\'s', 'The player\'s 7 cards. $5 or more earns an Envy button.',
                    One([['7-Card Straight Flush, no Joker', '2500 to 1'], ['Royal Flush + A-K Suited', '1000 to 1'], ['7-Card Straight Flush with Joker', '750 to 1'],
                    ['Five Aces', '250 to 1'], ['Royal Flush', '125 to 1'], ['Straight Flush', '50 to 1'], ['Four of a Kind', '25 to 1'],
                    ['Full House', '5 to 1'], ['Flush', '4 to 1'], ['Three of a Kind', '3 to 1'], ['Straight', '2 to 1']])),
                Tab('Envy', 'Envy', 'Paid to every OTHER hand with an Envy button when a player has one of these hands.',
                    One([['7-Card Straight Flush, no Joker', '$1,000'], ['Royal Flush + A-K Suited', '$750'], ['7-Card Straight Flush with Joker', '$250'],
                    ['Five Aces', '$100'], ['Royal Flush', '$50'], ['Straight Flush', '$20'], ['Four of a Kind', '$5']])),
                Tab('Progressive', 'Prog.', 'Two $1 progressives on the player\'s 7 cards. A percentage pays from the meter: supervisor required.', [
                    T('Emperor\'s Treasure $1', 'tl-h1', [['7-Card SF (no Joker)', '100%'], ['Royal + A-K Suited', '50%'], ['7-Card Straight Flush', '25%'],
                    ['Five Aces', '$1,000'], ['Royal Flush', '$175'], ['Straight Flush', '$150'], ['Four of a Kind', '$75'], ['Full House', '$5']]),
                    T('Insurance $1', 'tl-h1', [['9-High Same Color', '100%'], ['9-High Pai Gow', '$250'], ['10-High Pai Gow', '$125'], ['Jack-High Pai Gow', '$30'], ['Queen-High Pai Gow', '$7']])]),
                Tab('House Way', 'House Way', '', [], true)
            ]
        },
        {
            name: 'Player\'s Edge-21', tabs: [
                Tab('Bonus 21', 'Bonus 21', 'Bonus 21 payouts.',
                    One([['5-card 21', '3 to 2'], ['6-card 21', '2 to 1'], ['7-card 21', '3 to 1'], ['6-7-8 mixed suits', '3 to 2'], ['6-7-8 same suit', '2 to 1'], ['6-7-8 of Diamonds', '3 to 1'],
                    ['7-7-7 mixed suits', '3 to 2'], ['7-7-7 same suit', '2 to 1'], ['7-7-7 of Diamonds', '3 to 1']])),
                Tab('Match Up & Match Down', 'Match', 'Match Up and Match Down wagers.',
                    One([['2 Suited Matches', '18 to 1'], ['1 Non-Suited & 1 Suited Match', '13 to 1'], ['1 Suited Match', '9 to 1'], ['2 Non-Suited Matches', '8 to 1'], ['1 Non-Suited Match', '4 to 1']])),
                Tab('Monster Match', 'Prog.', 'Monster Match progressive: all 4 cards (the player\'s 2 and the dealer\'s 2). A percentage pays from the meter: supervisor required.', [
                    T('$5', 'tl-h5', [['Four of a Kind (Same Color)', '100%'], ['Four of a Kind', '$1,000'], ['Three of a Kind (Same Color)', '$100'], ['Three of a Kind', '$50'], ['Pair (Suited)', '$10']]),
                    T('$1', 'tl-h1', [['Four of a Kind (Same Color)', '100%'], ['Four of a Kind', '$200'], ['Three of a Kind (Same Color)', '$20'], ['Three of a Kind', '$10'], ['Pair (Suited)', '$2']])])
            ]
        },
        {
            name: 'Hold\'em', tabs: [
                Tab('Odds', 'Odds', 'Pays only when the player wins with a Straight or better; a smaller win pushes.',
                    One([['Royal Flush', '500 to 1'], ['Straight Flush', '50 to 1'], ['Four of a Kind', '10 to 1'], ['Full House', '3 to 1'], ['Flush', '3 to 2'], ['Straight', '1 to 1']])),
                Tab('Bad Beat', 'Bad Beat', 'Pays when the player\'s Straight or better loses, including a hand made on the board.',
                    One([['Straight Flush', '500 to 1'], ['Four of a Kind', '25 to 1'], ['Full House', '6 to 1'], ['Flush', '5 to 1'], ['Straight', '4 to 1']])),
                Tab('Trips Plus', 'Trips+', 'The player\'s best 5 of 7 cards.',
                    One([['Royal Flush', '100 to 1'], ['Straight Flush', '40 to 1'], ['Four of a Kind', '30 to 1'], ['Full House', '8 to 1'], ['Flush', '7 to 1'], ['Straight', '4 to 1'], ['Three of a Kind', '3 to 1']])),
                Tab('Pocket Bonus', 'Pocket', 'The player\'s 2 cards. Ace-Face = A-K, A-Q or A-J.',
                    One([['Pair of Aces', '25 to 1'], ['Ace-Face Suited', '20 to 1'], ['Ace-Face', '10 to 1'], ['Any Pair', '5 to 1']])),
                Tab('Progressive', 'Prog.', 'The player\'s best 5 of 7; a Royal on the board alone is a Community Royal. A percentage pays from the meter: supervisor required.', [
                    T('$5', 'tl-h5', [['Royal Flush', '100%'], ['Community Royal', '$1,000'], ['Straight Flush', '$400'], ['Four of a Kind', '$200'], ['Full House', '$20']]),
                    T('$1', 'tl-h1', [['Royal Flush', '100%'], ['Community Royal', '$200'], ['Straight Flush', '$80'], ['Four of a Kind', '$40'], ['Full House', '$4']])])
            ]
        }
    ];

    // Shared/HouseWayTable.razor: [heading] or [number, rule] or ['', letter, rule]
    var HOUSE_WAY = [
        ['Two Pair'],
        ['1', 'No Ace: split only if one pair is 7s or better.'],
        ['2', 'With an Ace: split only if Jacks or better AND 7s or better.'],
        ['3', 'Always split Aces and any other pair.'],
        ['Three Pair'],
        ['1', 'Always play the highest pair in front.'],
        ['Three of a Kind'],
        ['1', 'Always keep three of a kind together, unless they are Aces; then play a pair of Aces in back and one Ace in front.'],
        ['2', 'If the dealer has 3 Aces and at the same time has a straight or a flush, the straight or flush will be played.'],
        ['Straights, Flushes, Straight Flushes and Royal Flushes'],
        ['1', 'Keep as the high hand, except:'],
        ['', 'a', '6 and 7 card: play the low straight or flush to place the highest card in front.'],
        ['', 'b', 'Play a straight over a flush anytime the second highest hand will be improved. This includes straight flushes and royal flushes.'],
        ['', 'c', 'Play two pair (apply the two pair rule) except when both of the pairs are 6s or below.'],
        ['', 'd', 'With a Full House, apply the Full House rules.'],
        ['Full House'],
        ['1', 'Always split, playing the pair in front, unless the pair is 2s and you have an Ace and a King to play in front.'],
        ['2', 'With an extra pair, play the higher of the two pairs in front.'],
        ['Four of a Kind'],
        ['1', 'Always keep 6s and under together.'],
        ['2', 'Always split 7s through 10s, playing a pair in each hand, unless the hand contains an Ace or better to play in front.'],
        ['3', 'Always split Jacks through Kings, playing a pair in each hand.'],
        ['4', 'Split four Aces unless with another pair:'],
        ['', 'a', 'If the other pair is 6s or below, play Aces in front, two pair in back.'],
        ['', 'b', 'If the other pair is 7s or above, play four Aces in back, pair in front.'],
        ['Five Aces'],
        ['1', 'Split and play three in back and two in front, unless the hand also contains a pair of Kings; then play 5 Aces in back and Kings in front.'],
        ['Ranking of the Hands (highest first)'],
        ['1', 'Five Aces'], ['2', 'Royal Flush'], ['3', 'Straight Flush'], ['4', 'Four of a Kind'], ['5', 'Full House'], ['6', 'Flush'],
        ['7', 'Straight (the wheel, A-2-3-4-5, is the 2nd highest)'], ['8', 'Three of a Kind'], ['9', 'Two Pair'], ['10', 'One Pair'], ['11', 'High Card']
    ];

    var pay = { game: 0, tab: 0, hw: -1 };

    // House Way in sections (Two Pair, Three Pair, ...): a row of tabs picks
    // which section shows
    var HW_SHORT = ['Two Pair', 'Three Pair', 'Three of a Kind', 'Straights & Flushes', 'Full House', 'Four of a Kind', 'Five Aces', 'Ranking'];

    function hwSections() {
        var out = [];
        HOUSE_WAY.forEach(function (r) {
            if (r.length === 1) out.push({ title: r[0], rows: [] });
            else out[out.length - 1].rows.push(r);
        });
        return out;
    }

    // "All" (pay.hw = -1, the default) shows every section, like the main site
    function houseWayTable() {
        var secs = hwSections();
        var h = '<div class="lt-hw-tabs"><button type="button" class="lt-hw-tab' + (pay.hw < 0 ? ' tl-on' : '') + '" data-act="hw" data-i="-1">All</button>';
        secs.forEach(function (x, i) {
            h += '<button type="button" class="lt-hw-tab' + (i === pay.hw ? ' tl-on' : '') + '" data-act="hw" data-i="' + i + '">' + esc(HW_SHORT[i] || x.title) + '</button>';
        });
        h += '</div><table class="help-table sp-help-table hw-table">';
        secs.forEach(function (sec, i) {
            if (pay.hw >= 0 && pay.hw !== i) return;
            h += '<tr class="sp-pt-hd"><td colspan="2">' + esc(sec.title) + '</td></tr>';
            sec.rows.forEach(function (r) {
                if (r.length === 3) h += '<tr class="hw-sub"><td></td><td><b>' + esc(r[1]) + '</b>' + esc(r[2]) + '</td></tr>';
                else h += '<tr><td>' + esc(r[0]) + '</td><td>' + esc(r[1]) + '</td></tr>';
            });
        });
        return h + '</table>';
    }

    function payView() {
        var g = PAY[pay.game], tab = g.tabs[pay.tab], h;
        h = '<div class="tl-stage tl-full tl-stage-pt lt-stage">' +
            strip('Pay Tables', g.name + ' · ' + tab.name) +
            '<div class="tl-games lt-games">';
        PAY.forEach(function (pg, i) {
            h += '<button type="button" class="tl-game' + (i === pay.game ? ' tl-on' : '') + '" data-act="pgame2" data-i="' + i + '">' + esc(pg.name) + '</button>';
        });
        return h + '</div>' + payTabsBody(pay.game) + '</div>';
    }

    // One game's wager tabs and the open table (also used by the Table Scenes)
    function payTabsBody(gi) {
        var g = PAY[gi], tab = g.tabs[pay.tab], h = '<div class="tl-tabs">';
        g.tabs.forEach(function (t, i) {
            h += '<button type="button" class="tl-tab' + (i === pay.tab ? ' tl-on' : '') + '" data-act="ptab" data-i="' + i + '">' + esc(t.short) + '</button>';
        });
        h += '</div><div class="tl-pt-body">';
        if (tab.houseWay) {
            h += '<p class="hw-key">Front = Low hand (2 cards) &middot; Back = High hand (5 cards)</p>' + houseWayTable();
        } else {
            h += '<div class="tl-cols">';
            tab.tables.forEach(function (t) {
                h += '<div class="tl-col">' + (t.title ? '<div class="tl-col-hd ' + t.cls + '">' + esc(t.title) + '</div>' : '') + '<table class="tl-pt">';
                t.rows.forEach(function (r) { h += '<tr><td>' + esc(r[0]) + '</td><td>' + esc(r[1]) + '</td></tr>'; });
                h += '</table></div>';
            });
            h += '</div>' + (tab.note ? '<p class="tl-note">' + esc(tab.note) + '</p>' : '');
        }
        return h + '</div>';
    }

    // ---- Table Scenes: ready-made layouts per game ---------------------------
    // Left: the table limit ($10 / $300 to start, remembered per screen) and,
    // for Blackjack and Player's Edge-21, the Shuffle Timer (Pai Gow: the
    // Pai Gow Number). Right: the game's progressive touchscreen and its
    // pay tables.

    var TABLES = [
        { key: 'bj', name: 'Blackjack', text: 'Limit, Shuffle Timer, 21+3 progressive, pay tables', game: 0, pay: 0, side: 'timer' },
        { key: 'tc', name: 'Three Card Prime', text: 'Limit, Mini Royal progressive, pay tables', game: 1, pay: 1, side: '' },
        { key: 'pg', name: 'Pai Gow', text: 'Limit, Pai Gow Number, ET / INS progressives, pay tables', game: 2, pay: 2, side: 'pgn' },
        { key: 'hd', name: "Heads-Up Hold'em", text: 'Limit, progressive, pay tables', game: 4, pay: 4, side: '' },
        { key: 'pe', name: "Player's Edge-21", text: 'Limit, Shuffle Timer, Monster Match progressive, pay tables', game: 3, pay: 3, side: 'timer' }
    ];

    // Each screen's limit: indexes into MINS / MAXES ($10 / $300 to start)
    function tableLimit(key) {
        var p = (load('dst-table-limit-' + key) || '').split(',');
        var a = parseInt(p[0], 10), b = parseInt(p[1], 10);
        if (p.length === 2 && a >= 0 && a < MINS.length && b >= 0 && b < MAXES.length) return { min: a, max: b };
        return { min: 1, max: 1 };
    }

    function panel(title, body, cls, extra) {
        return '<div class="lw-p ' + (cls || '') + '"><div class="lw-hd"><span><b>' + esc(title) + '</b></span>' + (extra || '') + '</div><div class="lw-b">' + body + '</div></div>';
    }

    function tableView(ti) {
        var t = TABLES[ti], lim = tableLimit(t.key);
        var limitBody = '<div class="lw-limit">' +
            '<button type="button" data-act="tmin" data-key="' + t.key + '"><small>MINIMUM</small><b>$' + MINS[lim.min] + '</b></button>' +
            '<i></i>' +
            '<button type="button" data-act="tmax" data-key="' + t.key + '"><small>MAXIMUM</small><b>$' + MAXES[lim.max] + '</b></button></div>';

        var left = panel('Table Limit', limitBody, 'lw-lim' + (isLocked(t.key) ? ' lw-locked' : ''), lockBtn(t.key));
        if (t.side === 'timer') left += panel('Shuffle Timer', '<div class="lw-white">' + timerInner() + '</div>', 'lw-grow');
        if (t.side === 'pgn') left += panel('Pai Gow Number', '<div class="lw-white lt-pgn">' + pgnInner() + '</div>', 'lw-grow');

        var right = panel('Progressive Touchscreen · ' + PROG[t.game].name + ' — ' + progInstruction(), progTouchscreen(), 'lw-ts') +
            panel('Pay Tables · ' + PAY[t.pay].name, payTabsBody(t.pay), 'lw-pay lw-grow');

        return '<div class="tl-stage tl-full lt-stage lw">' +
            strip(t.name + ' Table', 'Table scene: tap any panel to use it') +
            '<div class="lw-grid"><div class="lw-col1">' + left + '</div><div class="lw-col2">' + right + '</div></div></div>';
    }

    // ---- Taps --------------------------------------------------------------

    app.addEventListener('click', function (e) {
        var el = e.target.closest ? e.target.closest('[data-act]') : null;
        if (!el || el.disabled) return;
        var a = el.getAttribute('data-act');
        var n = function (attr) { return parseInt(el.getAttribute(attr), 10); };

        switch (a) {
            case 'home': go('home'); return;
            case 'go': go(el.getAttribute('data-v')); return;

            // Table Scenes: the screen's own limit
            case 'lock':
                var lk = el.getAttribute('data-key');
                save('dst-lock-' + lk, isLocked(lk) ? '0' : '1');
                break;
            case 'tmin':
            case 'tmax':
                var tk = el.getAttribute('data-key'), tl = tableLimit(tk);
                if (isLocked(tk)) return;
                if (a === 'tmin') tl.min = (tl.min + 1) % MINS.length; else tl.max = (tl.max + 1) % MAXES.length;
                save('dst-table-limit-' + tk, tl.min + ',' + tl.max);
                break;

            // Limit Sign
            case 'min': if (isLocked('sign')) return; limit.min = (limit.min + 1) % MINS.length; save('dst-limit-sign', limit.min + ',' + limit.max); break;
            case 'max': if (isLocked('sign')) return; limit.max = (limit.max + 1) % MAXES.length; save('dst-limit-sign', limit.min + ',' + limit.max); break;

            // Progressive Touchscreen
            case 'pgame': prog.game = n('data-i'); progClear(); break;
            case 'pgames': prog.game = -1; break;
            case 'chip': var cw = n('data-w'), cs = n('data-s'); prog.on[cw][cs] = !prog.on[cw][cs]; break;
            case 'play': prog.screen = 'tables'; break;
            case 'endgame': progClear(); break;
            case 'payout': prog.w = prog.r = prog.p = -1; prog.screen = 'wager'; break;
            case 'selw': prog.w = n('data-w'); break;
            case 'next-rank': prog.r = -1; prog.screen = 'rank'; break;
            case 'selr': prog.r = n('data-i'); break;
            case 'next-pos': prog.p = -1; prog.screen = 'pos'; break;
            case 'selp': prog.p = n('data-s'); break;
            case 'next-confirm': prog.screen = 'confirm'; break;
            case 'confirm':
                if (prog.w >= 0 && prog.p > 0) prog.paid[prog.w][prog.p] = true;
                prog.w = prog.r = prog.p = -1;
                prog.screen = 'tables';
                break;
            case 'pback':
                prog.screen = prog.screen === 'confirm' ? 'pos' : prog.screen === 'pos' ? 'rank' : prog.screen === 'rank' ? 'wager' : 'tables';
                if (prog.screen === 'tables') prog.w = prog.r = prog.p = -1;
                break;

            // Player Tracking
            case 'dealer':
                track.note = null; track.loginFirst = false;
                if (!track.loggedIn) track.sel = track.sel === 0 ? null : 0;
                break;
            case 'seat':
                track.note = null;
                if (!track.loggedIn) { track.loginFirst = true; break; }
                var ss = n('data-s');
                track.sel = track.sel === ss ? null : ss;
                break;
            case 'in': track.entry = ''; track.screen = track.sel === 0 ? 'pin' : 'buyin'; break;
            case 'tback': trackBack(); break;
            case 'key': trackKey(el.getAttribute('data-k')); break;
            case 'del': if (track.entry.length) track.entry = track.entry.slice(0, -1); break;
            case 'ent': trackEnt(); break;
            case 'treset':
                track.buyins = [0, 0, 0, 0, 0, 0, 0];
                track.loggedIn = false; track.loginFirst = false; track.note = null;
                trackBack();
                break;

            // Shuffle Timer
            case 'timer': timerMain(); return;

            // Pai Gow Number
            case 'pgnmode': pgn.dice = el.getAttribute('data-d') === '1'; break;
            case 'roll': roll(); return;

            // Pay Tables
            case 'pgame2': pay.game = n('data-i'); pay.tab = 0; break;
            case 'ptab': pay.tab = n('data-i'); break;
            case 'hw': pay.hw = n('data-i'); break;
        }
        render();
    });

    render();
})();
