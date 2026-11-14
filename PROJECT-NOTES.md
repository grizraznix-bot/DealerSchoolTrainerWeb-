# Dealer School Trainer — Project Notes

Read this first when picking the project back up (new session, months
later, a pay table changed). It says where things live, the house rules
the site is built on, how changes are made, and the decisions that were
settled on purpose. Last updated: November 13, 2026.

---

## 1. What this is

- A Blazor WebAssembly (.NET 10) site on GitHub Pages:
  repo `grizraznix-bot/DealerSchoolTrainerWeb-`.
- Local folder:
  `C:\Users\jnix5\Documents\Excel Offline\WIP\Dealer School Trainer\DealerSchoolTrainerWeb`
- Purpose: practice for dealers in training (and working dealers learning
  more games) on off days and free time. Not required. Built around this
  house's pay tables and procedures on purpose. Hands-on skills (chip
  cutting, coloring up, shuffling) stay in person.
- Phones first (portrait), desktop works too (games sit in a centered
  520px column).

## 2. How changes are made

1. **Mockup first** for anything that changes the layout. Build only after
   the mockup is approved.
2. **Ask before writing** to the PC ("Send them to your PC?").
3. Jesse builds and pushes himself:

   ```
   dotnet build
   git status
   git add -A
   git commit -m "..."
   git push
   ```

   Every push to `main` deploys automatically (`.github/workflows/deploy.yml`);
   the live site updates in a couple of minutes. About → "Reload Latest
   Version" pulls it straight away.
4. **Test on a real phone** (the iPhone home-screen app). The browser's
   phone emulator is not a good stand-in.
5. Never present a guess as the fix. Say when a cause is unconfirmed.
6. "Notes for next session" are recorded only, not built in the same session.
7. Watch for stray copies like `File-1.cs` in the folder: duplicate files
   break the build and the site stops updating.

## 3. Where things live

| Folder | What's in it |
|---|---|
| `Pages/` | One page per game, tutorial and tool (see below) |
| `Games/<Game>/` | Game logic: dealing, hand evaluation, pay math |
| `Core/` | Cards, deck, session log, practice streak, shared helpers |
| `Shared/` | Reusable pieces: GameBar, keypad, CallRow, WagerPick, HouseWayTable, SessionSummary, ProgressivesLink, TileChoice |
| `Layout/MainLayout.razor` | Side menu (Games / Practice & Tools / Info) |
| `wwwroot/css/app.css` | All styling (one file) |
| `wwwroot/index.html` | Page shell + small scripts (old-device notice, double-tap fix, card preload, Reload Latest) |
| `wwwroot/manifest.webmanifest` | Home-screen app settings (`orientation: any`) |
| `wwwroot/lite/` | **Table Tools Lite** (`index.html`, `lite.js`, `lite.css`): plain HTML / JavaScript copies of the six Table Tools, the five Table Scenes and the About page for older iPads, iPad sideways only. Borrows `css/app.css` for the look |

**Pages**

| Page | Route |
|---|---|
| Home | `/` |
| Blackjack (Flash Cards, Payouts 3:2 / 6:5, Side Wagers) + tutorial | `blackjack-trainer`, `blackjack-tutorial` |
| Three Card Prime + tutorial | `three-card-trainer`, `three-card-tutorial` |
| Pai Gow (Hand Setting, Settling Bets) + tutorial | `pai-gow`, `pai-gow-tutorial` |
| Heads-Up Hold'em + tutorial | `holdem-trainer`, `holdem-tutorial` |
| Roulette + tutorial, Roulette math tricks | `roulette`, `roulette-tutorial`, `roulette-math` |
| Progressives (Practice / Touchscreen only) + tutorial | `progressives`, `progressives-tutorial` |
| Challenge Mode | `challenge` |
| Table Scenes list (the Table Scenes button on Table Tools) | `tools/scenes` (`Pages/ToolTableScenes.razor`) |
| Table Scenes (ready-made per game: `bj`, `tc`, `pg`, `hd`, `pe`) | `tools/table/bj` etc. (`Pages/ToolTableScreen.razor` + `Shared/Ts*.razor`) |
| Table Tools: Limit Sign, Pay Tables, Shuffle Timer, Pai Gow Number, Player Tracking | `tools`, `tools/limit-sign`, `tools/pay-tables`, `tools/shuffle-timer`, `tools/pai-gow-number`, `tools/player-tracking` |
| About, Add to Home Screen | `about`, `add-to-home-screen` |

## 4. Changing a pay table — every place to check

Pay tables are **not** in one place. Game math lives in `Games/`, but the
Help tables, tutorials and the Pay Tables tool are written out by hand.
When a pay changes, update **all** of these, then search the project for
the old number to catch anything missed.

| Wager | Game math | Shown in |
|---|---|---|
| 21+3, Lucky Ladies | `Games/SideWagerTrainer/SideWagerPayouts.cs` | `BlackjackTrainer.razor` (Help), `BlackjackTutorial.razor`, `Games/ToolData/PayTablesData.cs` (Pay Tables tool and Table Scenes) |
| Three Card Prime (Ante Bonus, Pair Bonus, Prime, All 6) | `Games/ThreeCardTrainer/ThreeCardPayouts.cs` | `ThreeCardTrainer.razor` (Help), `ThreeCardTutorial.razor`, `Games/ToolData/PayTablesData.cs` (Pay Tables tool and Table Scenes) |
| Pai Gow (Insurance, Emperor's Treasure, Envy) | `Games/PaiGowTrainer/PaiGowPayouts.cs` | `PaiGowCombined.razor` (Help), `PaiGowTutorial.razor`, `Games/ToolData/PayTablesData.cs` (Pay Tables tool and Table Scenes) |
| Pai Gow House Way | `Games/PaiGowTrainer/PaiGowSolver.cs` | `Shared/HouseWayTable.razor` (Help and the Pay Tables tool), and `wwwroot/lite/lite.js` |
| Hold'em (Odds, Bad Beat, Trips Plus, Pocket Bonus) | `Games/HoldemTrainer/HoldemPayouts.cs` | `HoldemTrainer.razor` (Help), `HoldemTutorial.razor`, `Games/ToolData/PayTablesData.cs` (Pay Tables tool and Table Scenes) |
| Blackjack 3:2 / 6:5 | `Games/PayoutTrainer/PayoutWager.cs` | `BlackjackTrainer.razor`, `Challenge.razor`, `BlackjackTutorial.razor` |
| Roulette | `Games/RouletteTrainer/` | `RouletteTrainer.razor`, `RouletteTutorial.razor` |
| **All progressives** (Blackjack 21+3, Three Card Mini Royal, Pai Gow ET / INS, Player's Edge-21 Monster Match, Hold'em) | `Games/ProgressiveTrainer/ProgressiveGames.cs` (one table per wager, used by both Practice and Touchscreen only) | `ProgressivesTutorial.razor` (example table, "what each game pays on"), `Games/ToolData/PayTablesData.cs` (Pay Tables tool and Table Scenes) |
| Player's Edge-21 Bonus 21 / Match | — (no game mode) | `Games/ToolData/PayTablesData.cs` (Pay Tables tool and Table Scenes) only |
| **Every Table Tools pay table and every progressive** | — | also `wwwroot/lite/lite.js` (Table Tools Lite) |
| About text and Special Thanks | `Pages/About.razor` | also `wwwroot/lite/lite.js` (`aboutView`, Lite's About button) |

Idea on the list for later: one **house-rules file** that every page reads,
so a pay change is one edit.

## 5. House rules the site is built on

**Settling order** (progressives always after the game's other wagers)
- Blackjack: once all cards are dealt, 21+3, then Lucky Ladies, then
  progressives, then the game. Side wagers are settled before the dealer
  checks for blackjack, except with a possible Q♥ pair + dealer blackjack:
  supervisor-approved peek.
- Three Card Prime: Play, Ante (+ Ante Bonus), Pair Bonus, Prime, All 6.
- Pai Gow: Insurance, Main Wager, Emperor's Treasure.
- Heads-Up Hold'em: Raise, Ante / Odds, Trips Plus, Pocket Bonus.

**Pay tables**
- 21+3: SF 30, Trips 20, Straight 10, Flush 5.
- Lucky Ladies: Q♥ pair + dealer BJ 1000, Q♥ pair 125, matched 20 19,
  suited 20 9, any 20 4.
- Three Card Prime: Play and Ante 1 to 1; Ante Bonus SF 5, Trips 4,
  Straight 1; Pair Bonus SF 40, Trips 30, Straight 6, Flush 3, Pair 1;
  Prime 3 same color 3 to 1, all 6 same color 4 to 1; All 6 Royal 1000,
  SF 200, Quads 50, Full House 25, Flush 20, Straight 10, Trips 5.
- Pai Gow: Main 1 to 1, no commission; dealer Ace-high Pai Gow pushes all.
  Insurance 9-high 100, 10-high 25, J 15, Q 6, K 5, A 3 (Joker = Ace).
  Emperor's Treasure 7-card SF no Joker 2500, Royal + A-K suited 1000,
  7-card SF with Joker 750, 5 Aces 250, Royal 125, SF 50, Quads 25,
  Full House 5, Flush 4, Trips 3, Straight 2. Envy ($5+ on ET, pays
  other players only) $1000 / $750 / $250 / $100 / $50 / $20 / $5.
- Heads-Up Hold'em: Raise 1 to 1 (3x / 2x / 1x Ante); Ante 1 to 1, pushes
  if dealer doesn't open; Odds (player win, Straight+) Royal 500, SF 50,
  Quads 10, Full House 3, Flush 3 to 2, Straight 1; Bad Beat SF 500,
  Quads 25, Full House 6, Flush 5, Straight 4; Trips Plus Royal 100, SF 40,
  Quads 30, Full House 8, Flush 7, Straight 4, Trips 3; Pocket Bonus Aces
  25, Ace-Face suited 20, Ace-Face 10, Pair 5.
- Player's Edge-21: Bonus 21 and Match Up / Match Down tables in the Pay
  Tables tool; Monster Match progressive uses the dealer's first 2 and the
  player's first 2 cards only.
- Progressives ($5 / $1): see `ProgressiveGames.cs`. Any metered pay
  (100%, 50%, 25%) = Call Supervisor in the trainer. Dynamic Pays
  multipliers are deliberately left out.
- 6:5 Blackjack is only the house's Single Deck game, shown as an option
  in Payouts and Challenge Mode ("Single Deck Blackjack only"). The
  Dealer School teaches 3:2.

**Progressive touchscreen routine**: check the wagers are right and the
positions are lit → PLAY → collect and lock up the chips → deal → PAYOUT
(wager, rank, position, pay, CONFIRM), once per winning wager, only unpaid
wagers listed the second time → END GAME every round. Lit $5 = red,
lit $1 = blue.

## 6. Decisions — keep these

- Every wrong answer reads "INCORRECT — Call the floor" (not in Flash
  Cards or Challenge Mode). Mistakes are never fixed alone.
- Call Only / Call & Pay modes; Call Only is the default.
- Standard 3-column phone keypad; cards never shrink to make room for it.
- One player hand per mode.
- Session summary stays encouraging, not a hiring judgment.
- Phones are portrait, except Progressives Touchscreen only and the Table
  Tools, which go full screen sideways.
- Player Tracking is a demo only: any PIN logs in as "Dealer", players
  show as "Guest", Reset clears everything.
- Home sections: Dealer School course (1 Blackjack, 2 Three Card Prime,
  3 Pai Gow), Beyond Dealer School (Hold'em, Roulette), Practice & Tools
  in gold (Progressives, Challenge Mode, Table Tools). No separate Single
  Deck Blackjack card.
- Generic labels on the simulated terminals, no vendor names or logos.
  Disclaimer on Home and About, and a "Training simulation only" line on
  the simulated terminals (Progressives, Player Tracking, Table Scenes,
  every Lite screen).
- Table Scenes (main site and Lite): ready-made per game. Table limit
  starts $10 / $300 and **locked** (taps do nothing until unlocked; the
  Limit Sign has the same lock); limit and lock remembered per screen.
  Shuffle Timer only on Blackjack and Player's Edge-21, Pai Gow Number on
  Pai Gow, nothing under the limit on Three Card and Hold'em. The
  touchscreen panel has a fixed height so it never resizes between steps.
  Rank lists read top to bottom (Pai Gow ET: two columns, read down).
  Tablet / computer: two columns; phone: stacked and scrolling.
- House Way (`Shared/HouseWayTable.razor`, and Lite) has section tabs:
  All (default), Two Pair, Three Pair, Three of a Kind, Straights &
  Flushes, Full House, Four of a Kind, Five Aces, Ranking.
- Some Table Scene pieces repeat the single tools' code: `Shared/TsTimer`
  (Shuffle Timer), `Shared/TsPaiGowNumber` (Pai Gow Number) and
  `Shared/TsTouchscreen` (the Progressives page's Touchscreen only mode).
  A change to one of those tools should be made in its Table Scene copy
  (and Lite) too.
- iPhone Safari text scaling turned off (`text-size-adjust: 100%`).

**Rejected — don't suggest again**: 4-column keypad; Three Card wagers in
a row between hands or in slide-out / drawer / pull-up panels; "audition
ready" steps in the summary; swipe gestures; multiple player hands; Pai
Gow vertical labels, larger cards or rank bubble in Call Only; Flash
Cards Numbers Only.

## 7. Devices

- Needs WebAssembly with SIMD: iOS / iPadOS 16.4+, or a current Chrome,
  Edge, Firefox or Safari. Older devices (e.g. iPad Air 2, iPadOS 15.8.8)
  get a green "This device can't run…" notice (`index.html`). Confirmed on
  the iPad Air 2.
- Decided: nothing is removed or switched off to support older devices.
- **Table Tools Lite** (`.../lite/`) covers the training-table tools on
  older iPads: Limit Sign, Progressive Touchscreen (all five games), Player
  Tracking, Shuffle Timer, Pai Gow Number, Pay Tables. iPad sideways only.
  The old-device notice links to it. Lite also has five ready-made
  **Table Scenes** (Blackjack, Three Card Prime, Pai Gow, Heads-Up
  Hold'em, Player's Edge-21): table limit ($10 / $300 to start, remembered
  per screen) top left; under it the Shuffle Timer (Blackjack, Player's
  Edge-21) or the Pai Gow Number (Pai Gow), blank for the others; that
  game's progressive touchscreen and pay tables on the right. The main
  site has the same five Table Scenes (Table Tools page → Table Scenes button). **Any change to a Table Tool, or to a
  pay table shown in one, must be repeated in `wwwroot/lite/lite.js`.**
- A native Android / iOS app was considered and set aside: staying a website.

## 8. Still to do / later

- Once the site is complete: look at protecting the site (private repo,
  copyright notice, host with a login) and getting management / compliance
  sign-off.
- Older ideas, not scheduled: procedure-order unlock; house-rules settings
  file; Foul Frequency label "Rare"; Drills C and D; automatic version
  check; chip-tray Payouts; shuffle and cheque-handling tutorials; delete
  unused images (HouseWays.png, SettlingBetsWagerRules.png,
  HandRankings.png).
