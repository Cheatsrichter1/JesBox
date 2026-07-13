# JesBox

A Jackbox/Kahoot-style party game with a Christian trivia theme:
- **Unity** is the "TV" — one big screen that everyone in the room watches. It hosts the room, runs the quiz, and shows the scoreboard.
- **React (phone-ui)** is the controller — each player opens a web page on their own phone, joins with a 4-letter room code, and taps answers.
- **Node server** is the relay in between — it hands out room codes and forwards messages between the Unity host and every player's phone. It also serves the built phone-ui as static files, so the whole thing is one process to deploy.

The host (TV/Unity) picks one of six game modes from the lobby screen before hitting Start:

- **Trivia Quiz** — Bible trivia buzzer round. Selectable difficulty (Easy/Medium/Hard, ~8 questions each), question count (3–10), and per-question time limit (5–30s), all adjustable with +/- steppers in the lobby. Speed-bonus scoring, live scoreboard.
- **Microgames** — WarioWare-style rapid-fire rounds (2–8 rounds, picked at random from the bank) that everyone plays at once on their own phone: *Manna Rain* (mash-to-tap race), *Fishers of Men* (tap the fish, avoid the tridents), *Joyful Noise* (shake your phone like a tambourine, using the accelerometer with a tap fallback), *Walk on Water* (tilt or drag to dodge falling waves), *Loaves and Fishes* (tap when the sweeping marker hits the target zone), and *Parting the Waters* (swipe alternating left/right). All but Manna Rain and Joyful Noise run their reflex gameplay client-side and submit a final score.
- **Prompt & Vote** — Quiplash-style multiple-choice scenario voting (2–8 prompts). Everyone votes for one of 4 preset responses to a scenario; whoever matches the crowd favorite gets the bonus, everyone else gets a small participation score.
- **Chosen One** — true WarioWare-style solo spotlight (2–12 turns, host-adjustable). Each turn a random player (never repeating until everyone's had a turn) is handed a phone controller — everyone else's phone just shows who's up — while the actual minigame renders live on the TV. Every game is a quick (3–6s) single pass/fail challenge, not a scored timer: *Fiery Furnace Dash* (dodge 3 falling flames across 3 lanes, one hit and you're out), *David's Slingshot* (one shot to hit a swinging Goliath), *Joyful Prayer* (shake the phone fast enough to fill the meter in time), *Loaves and Fishes* (one tap to catch a sweeping target in its zone), and *Parting the Sea* (tilt the phone left/right like a Wii remote — or drag a touch track — swinging fully side to side fast enough to hit the target count). A win pays 500 points, a fail pays 0 — no partial credit. Gameplay state (positions, hits, meters) lives entirely in `GameManager`, driven in real time by `move`/`fire`/`shake`/`steer` messages from the one chosen phone. Each turn opens with a big flashed command (`DODGE!`, `FIRE!`, `SHAKE!`, ...) and a screen-flash/stinger beat before the timer starts, closes with an instant win/fail stamp (✓/✗) before cutting to the scoreboard, and the whole mode ramps up turn over turn — flames/targets move faster and the time window shrinks (floor ~65% of the base duration) the further into the set you get. The stage itself fills almost the entire screen (`SoloStageDefaultSize`/`Pos` in `GameManager.cs`, currently 1910×1060 out of the 1920×1080 canvas — a ~10px frame on every side) — the "Turn X/Y" / title / cheer prompt only shows for that opening beat (`SetSoloPromptVisible`) and then hides, and the countdown timer (added after the stage, so it always renders on top) sits right at the bottom edge as a small HUD overlay rather than in its own reserved strip, matching WarioWare's presentation: flash the prompt, get out of the way, show the timer, cut back to the scoreboard when it's done.
- **Sketch & Guess** — a separate mode (2–10 rounds, host-adjustable), same fair-rotation picking as Chosen One but its own two-phase flow: the chosen player draws a secret Bible-themed prompt on their phone — 7 colors and 3 brush sizes to pick from — and strokes stream to the TV and render live as ink segments, then everyone else picks the answer from 4 options. Correct guessers get 500 each, and the artist gets a 150-point bonus per correct guesser.
- **Bible Charades** — the last mode (2–10 rounds, host-adjustable), same fair-rotation picking as Chosen One and Sketch & Guess. Each round is randomly either an **Act** round (classic silent charades) or a **Describe** round (Activity/Taboo-style — a short list of forbidden words the performer can't say). Only the performer's phone ever sees the secret (via the targeted `game_to` relay) — the TV just shows generic "act it out" / "describe it, don't say the words" instructions so nothing leaks to the room. Everyone else watches/listens live in the room (nothing streams to the TV), then guesses the answer from 4 options on their phone, exactly like Sketch & Guess's guess phase. Correct guessers get 500 each, and the performer gets a 150-point bonus per correct guesser. Content bank: `CharadePrompts.cs`, 14 bilingual entries.

## QR code join

The lobby screen shows a scannable QR code next to the room code and URL (top-left, mirroring the language chips top-right). It's generated by calling the free `api.qrserver.com` HTTP API with the join URL (`<join-url>/?code=ROOMCODE`) and displaying the returned PNG on a `RawImage` — no QR-encoding logic lives in this project. It refetches whenever the room code or language changes (`ApplyOrFetchQr()`/`FetchQrCode()` in `GameManager.cs`). Scanning it opens the phone-ui with the room code pre-filled, since `JoinScreen.jsx` reads a `?code=` query param on load.

## Sound Manager

`Assets/Scripts/Game/SoundManager.cs` adds short audio cues (player join, round start, per-second countdown ticks/beeps, reveal, victory fanfare) at the key moments in every game mode. Every tone is synthesized procedurally at runtime (sine waves via `AudioClip.Create`, cached per cue) — there are no audio asset files to import, so sound works immediately on a fresh clone.

**Drop in your own audio**: same Resources-folder pattern as the Chosen One visuals below. Put a clip named after the cue at `Assets/Resources/Sounds/{key}.wav` (`.mp3`/`.ogg` work too) — the cue keys are `click`, `join`, `roundstart`, `tick`, `countdown`, `reveal`, `victory`, `go`, `success`, `fail` (each one's usage is named in its `Play*` method in `SoundManager.cs`). `Tone()` checks there first and uses your clip instead of synthesizing one — no code changes needed, and cues you haven't replaced yet keep working procedurally.

## Chosen One visuals (unique art per minigame)

Every Chosen One game used to render as plain colored UI rectangles, all built the same way. Two of them — **Joyful Prayer** and **Parting the Sea** — now go through a swappable visual layer instead, so each game can have its own distinct art style (2D sprites, a 3D diorama, whatever fits) without GameManager needing to know or care which:

- **`Assets/Scripts/Game/SoloGameVisuals.cs`** defines `ISoloGameVisual` (`Setup(stage)` / `SetProgress(fraction)` / `Teardown()`) — GameManager still owns 100% of the actual game logic (counting shakes/taps, deciding win/lose); the visual is purely cosmetic, driven by a 0–1 progress value. A visual can additionally implement `ISteerableSoloGameVisual` (`Pulse()`) for a one-off reaction each time the player's input actually counts for something, rather than a continuous fill — Parting the Sea uses this to bounce its cone.
- **`SoloGameVisualFactory.Create(kind)`** first looks for a hand-authored prefab at `Assets/Resources/SoloVisuals/{KindName}.prefab` (e.g. `SoloVisuals/PartingTheSea.prefab`). **This is the drop-in point for your own art** — build a prefab in the Editor with your own meshes/sprites/animations, save it there, and it's picked up automatically next time that game is chosen. No code changes needed. Two ways to make a prefab work:
  1. **No script at all.** Just build the scene — for `PartingTheSea` specifically, a camera named exactly `RenderCam`, two objects named `WaterFront1`/`WaterFront2`, and (optionally) one named `Cone`. `PartingTheSeaCustomVisual` gets attached to the prefab automatically at runtime and finds those by name anywhere in the hierarchy: it redirects the camera to a `RenderTexture` shown on the stage (sized to match the shared Chosen One stage — see `RenderWidth`/`RenderHeight` in `PartingTheSeaCustomVisual`, matching `SoloStageDefaultSize` in `GameManager.cs`), nudges the two water fronts apart as progress increases (tune `MaxSpread` in `PartingTheSeaCustomVisual` if that's too subtle or too much for your scene's scale/camera distance), and — on each swing the player successfully lands, not continuously — bounces the cone one world unit to the right and back (`PulseDistance`/`PulseOutTime`/`PulseBackTime`), using the cone's *world* position so it's unaffected by whatever local rotation it has in your hierarchy. Everything else in the prefab (ground, sea surface, lighting, decoration) is left exactly as you built it. This is the easiest path — no C# knowledge needed, just Editor work.
  2. **Your own script.** Give the prefab's root a component implementing `ISoloGameVisual` (and `ISteerableSoloGameVisual` if it has something to pulse) yourself for full control over the animation — this is used as-is instead of any adapter.
- If neither applies (no prefab, or a prefab with neither an adapter nor its own script), it falls back to a built-in placeholder so the game never looks broken/blank:
  - **`JoyfulPrayerVisual`** — a 2D example: a soft procedurally-generated glow that grows and warms in color as the shake meter fills. Has a `customSprite` field (visible in the Inspector if you attach/prefab it yourself) to swap in real painted art.
  - **`PartingTheSeaVisual`** — a 3D example: a small low-poly-primitive diorama (floor, two "water" walls that slide apart as progress increases, a directional light) rendered by its own dedicated camera into a `RenderTexture`, which is what actually gets displayed on the stage via a `RawImage`. The rest of the game is a UI Canvas with no camera of its own, so this doesn't conflict with anything. Has a `customWaterWallPrefab` field to swap in real low-poly meshes/materials for the walls.

The other three Chosen One games (Fiery Furnace Dash, David's Slingshot, Loaves and Fishes) still use the original plain-rectangle stage code in `GameManager.cs` — the same prefab-or-placeholder pattern extends to them the same way if you want unique art there too (add a case to `SoloGameVisualFactory.WrapPrefab` for a script-free adapter, or just give your prefab its own `ISoloGameVisual` script), it just hasn't been done yet.

## Language (English / German)

The TV and each phone pick their language **independently**:

- **TV**: an EN/DE chip pair in the top-right of the lobby screen (`Assets/Scripts/Game/Localization.cs`, class `L`). Changing it rebuilds the lobby panel in place and re-picks which language variant of every content bank (trivia questions, microgame/solo/vote/draw-prompt text) gets broadcast from then on. All 24 trivia questions, all microgame/solo-game titles and instructions, all 8 vote prompts, and all 14 draw prompts have full English and German text — see the `*En`/`*De` field pairs and `Question`/`Title`/`Scenario`/`Answer`/etc. computed properties on each content class.
- **Phone**: an EN/DE toggle fixed in the top-right corner of every screen (`phone-ui/src/i18n.jsx`, `LanguageProvider`/`useLanguage`), persisted to `localStorage`. This only translates the phone's own chrome (buttons, labels, "waiting for others" text) — the actual question/prompt/instruction text always arrives from Unity already in whatever language the host picked, so if the host is running the TV in German and a player picks English chrome, they'll see English buttons around German trivia questions. That's intentional: the two pickers solve different problems (host's content language vs. an individual guest's UI language) and don't need to agree.

## Repo layout

```
Assets/Scripts/Net/     Unity WebSocket client + message contracts
Assets/Scripts/Game/    Unity GameManager (mode state machines) + trivia/microgame/vote content banks
Assets/Scripts/UI/      Unity runtime UI builder (no manual scene wiring needed)
server/                 Node.js relay server + static host for phone-ui
phone-ui/               React (Vite) phone controller app
```

## How the pieces talk to each other

Everything is plain JSON over one WebSocket per client, relayed by the server:

- Unity connects first and sends `{"type":"create_room"}` → server replies with a room code.
- Each phone connects and sends `{"type":"join","roomCode":"ABCD","name":"..."}` → server assigns a `playerId` and tells the host.
- From then on, both sides just send `{"type":"game","data":{...}}`. The server doesn't understand `data` at all — it just relays host→all-players or player→host. All game rules (questions, timing, scoring) live in Unity; the phones are dumb display+input surfaces (except the reflex microgames — Fishers of Men, Walk on Water, Loaves and Fishes, Parting the Waters — whose gameplay runs client-side and reports a final score).
- There's one more message shape, `{"type":"game_to","playerId":"p3","data":{...}}`, host-only, relayed to exactly one player instead of the whole room. It exists for secrets that only one phone should ever see — Sketch & Guess's answer, and Bible Charades' prompt/forbidden-words.

See `server/index.js` for the exact relay rules and `Assets/Scripts/Net/Messages.cs` for every payload shape, keyed by `phase`: `lobby`, `question`/`reveal` (trivia), `microgame`/`microgame_reveal` (microgames), `vote_prompt`/`vote_reveal` (prompt & vote), `solo_turn`/`solo_reveal` (Chosen One), `sketch_draw`/`sketch_answer`/`sketch_guess` (Sketch & Guess, which reuses `solo_reveal` for its own reveal), `charade_turn`/`charade_secret`/`charade_guess` (Bible Charades, which also reuses `solo_reveal`), and `final`. Phone→host input is a single generic shape, `{"action": "answer"|"vote"|"tap"|"submit_score"|"move"|"fire"|"shake"|"steer"|"draw_point"|"draw_clear", "choice": n, "value": n, "x": n, "y": n, "newStroke": bool}` — for Chosen One, Sketch & Guess, and Bible Charades, only the currently-chosen player's phone sends `move`/`fire`/`shake`/`steer`/`draw_point`/`draw_clear`, and the host ignores those actions from anyone else. `steer` (Parting the Sea's tilt/drag control) reuses the `x` field for a continuous -1..1 lean value rather than a discrete choice. Sketch and Charades guesses both reuse the plain `answer` action.

## Reconnection

If a phone's WebSocket drops mid-game (network blip, backgrounded tab, accidental reload), it can pick back up as the *same* player — same `playerId`, same score, since the host keeps score keyed by `playerId` and never touches it during a drop — instead of joining fresh with 0 points:

- **Server** (`server/index.js`): on a player socket closing, the room doesn't delete that player immediately. It nulls out their `ws`, tells the host `player_disconnected`, and holds the slot open for `RECONNECT_GRACE_MS` (60s). A new `rejoin` message (`{"type":"rejoin","roomCode":"ABCD","playerId":"p3"}`) within that window reclaims the slot and tells the host `player_reconnected`. If the grace period lapses first, the slot is torn down for real and the host gets the ordinary `player_left`.
- **Host** (`GameManager.cs`): `player_disconnected`/`player_reconnected` just flip a `Disconnected` flag (shown as "(reconnecting...)" in the TV's lobby player list) — the player's `PlayerState`/score is never removed, so nothing about scoring changes. The one real piece of logic is resync: every room-wide broadcast is stashed as raw JSON (`BroadcastGame<T>`) and every per-player secret (Sketch & Guess's answer, Bible Charades' prompt) is stashed keyed by player (`SendToPlayer<T>`); on `player_reconnected`, `ResyncPlayer` resends both to that one phone via `game_to`, with the timer field (`duration`/`timeLimit`) patched to the *actual* remaining time rather than the original full duration — so a phone that reconnects mid-question lands right back on the current question with a correct countdown, not a blank screen waiting for the next phase.
- **Phone** (`phone-ui/src/App.jsx`): the room code + playerId + name are saved to `sessionStorage` on join. Any unexpected socket close (with an active session) triggers an automatic `rejoin` attempt with exponential backoff (up to 5 tries), showing a "Reconnecting..." spinner instead of dumping the player back to the join screen. A page reload does the same thing on mount. If every retry fails or the server reports the session's expired, it falls back cleanly to the normal join screen with an explanation.

This only covers a *player's* phone dropping — if the host (TV/Unity) itself disconnects, the room is torn down immediately (`host_left`) and everyone has to wait for it to relaunch and recreate the room, since all game state lives in that one Unity process.

## Host controls

A small admin menu bar sits in the bottom-right corner of the TV screen. It's only ever visible in two situations — the lobby (game selection), and mid-game after pressing **Esc** — so it never sits over the now-fullscreen games otherwise:

- **Esc** pauses whatever's running (`GameManager.Update()`, guarded to only fire while a game is actually running) and reveals the bar; pressing it again resumes and hides the bar. This is the only way to reach Pause/Skip/End Game mid-game — there's no persistent button for it, on purpose, since the games are meant to fill the screen uninterrupted.
- **Pause / Resume** — freezes every round's timer (and any live gameplay driven by it — falling flames, moving targets, drawing/guessing countdowns) in place. Internally every round loop reads elapsed time through a single `Dt()` helper that returns `0` while paused, so nothing needs its own pause-awareness. Every phone gets a small "⏸ Game Paused" banner overlaid on top of whatever it's currently showing (via a `pause_state` broadcast intercepted the same way targeted secrets are) — the underlying screen doesn't change, so resuming just picks up exactly where it left off.
- **Skip** — force-ends whichever round is currently in progress (question, microgame, vote, Chosen One turn, Sketch & Guess draw/guess phase, Bible Charades turn/guess phase) right now, same as if its timer had run out.
- **End Game** — force-ends the current round *and* stops the mode from starting another one, dropping straight to the final scoreboard.
- **Players** — toggles an overlay listing every current player (including anyone mid-reconnect-grace-period) with score and a **Kick** button. Kicking sends a host-only `kick_player` message; the server closes that player's socket immediately and drops their slot for good — unlike an ordinary disconnect, a kicked player is not eligible to rejoin with the same identity.

Pause/Skip/End Game only ever show while paused mid-game (there's nothing to skip/end from the lobby). Players shows both in the lobby and while paused mid-game, and auto-closes/hides the moment the game resumes or a fresh one starts.

## Running it locally (dev loop)

You need 3 things running at once while developing:

1. **Server** (relay + static host):
   ```
   cd server
   npm install
   npm start
   ```
   Runs on `http://localhost:8080`, WebSocket at `/ws`.

2. **Phone UI** (hot-reloading dev server, proxies `/ws` to the server above):
   ```
   cd phone-ui
   npm install
   npm run dev
   ```
   Open the printed `localhost:5173` URL on your phone (same Wi-Fi) or in a browser tab to act as a player.

3. **Unity**: open the project in Unity Hub (6000.3.12f1). On first open it will fetch the NativeWebSocket and Newtonsoft Json packages — wait for that to finish. Then:
   - In `SampleScene`, create an empty GameObject named `GameManager`.
   - Add the `GameManager` component (`Assets/Scripts/Game/GameManager.cs`) to it.
   - In the Inspector, set **Server Url** to `ws://localhost:8080/ws`.
   - Press Play. The Game view becomes the TV screen: it shows a room code once connected.
   - Join from a phone/browser using that code, then hit **Start Game** on the TV once at least one player has joined.

## Building for real use

1. Build the phone UI once (the server serves these static files — no need to keep `npm run dev` open):
   ```
   cd phone-ui
   npm run build
   ```
   This produces `phone-ui/dist`, which `server/index.js` serves automatically.

2. Run the server (`cd server && npm start`, or see deployment below).

3. In Unity, set **Server Url** on the `GameManager` to your real address, e.g. `wss://n8n.flow36.de/ws` (use `wss://` once your server has HTTPS, which most reverse proxies/hosting will require for phones to load the page over `https://` in the first place).

4. Build the Unity project as a standalone build (File → Build Profiles → Windows/Mac, Build) — that's the executable you run on the TV/host machine. It does not need to be hosted; it just needs network access to your server.

## Deploying to your server

This is git-based: push to a remote, then pull + build + restart on the server. This repo is already a git repo; you just need to add a remote (GitHub/GitLab/etc. or a bare repo on the server itself) and push to it.

### One-time server setup (Ubuntu)

SSH into the server for all of this.

**1. Prerequisites + Node.js 22 (via NodeSource) + nginx:**
```
sudo apt update && sudo apt upgrade -y
sudo apt install -y git curl nginx
curl -fsSL https://deb.nodesource.com/setup_22.x | sudo -E bash -
sudo apt install -y nodejs
node -v   # sanity check
sudo npm install -g pm2
```

**2. Clone and build:**
```
git clone https://github.com/Cheatsrichter1/JesBox.git ~/jesbox
cd ~/jesbox/server && npm install --omit=dev
cd ~/jesbox/phone-ui && npm install && npm run build
```

**3. Start the server with pm2 and make it survive reboots:**
```
cd ~/jesbox/server
pm2 start ecosystem.config.js
pm2 save
pm2 startup   # prints a systemd command — copy/paste & run the line it gives you
```

**4. Firewall:**
```
sudo ufw allow OpenSSH
sudo ufw allow 'Nginx Full'
sudo ufw enable
```

**5. nginx reverse proxy** — create `/etc/nginx/sites-available/jesbox`:
```
server {
    listen 80;
    server_name n8n.flow36.de;

    location / {
        proxy_pass http://127.0.0.1:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }

    location /ws {
        proxy_pass http://127.0.0.1:8080;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
    }
}
```
Then enable it:
```
sudo ln -s /etc/nginx/sites-available/jesbox /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

**6. DNS**: point an A record for `n8n.flow36.de` at the server's public IP, and wait for it to resolve.

**7. TLS (Let's Encrypt)** — once DNS resolves:
```
sudo apt install -y certbot python3-certbot-nginx
sudo certbot --nginx -d n8n.flow36.de
```
This rewrites the nginx config to add HTTPS + an http→https redirect, and sets up auto-renewal.

**8. Test**: visit `https://n8n.flow36.de/healthz` (should say `ok`) and `https://n8n.flow36.de/` (should show the phone join screen). Point the Unity build's **Server Url** field at `wss://n8n.flow36.de/ws`.

**9. Passwordless deploys** — from your own machine, so `scripts/deploy.sh` doesn't prompt for a password every time:
```
ssh-copy-id user@n8n.flow36.de
```
Then set `SERVER_HOST="user@n8n.flow36.de"` and `SERVER_PATH="/home/user/jesbox"` in `scripts/deploy.sh`.

**Every update after that**: edit `scripts/deploy.sh` once to set `SERVER_HOST` and `SERVER_PATH` (the path you cloned into above, e.g. `/home/user/jesbox`), then just run:

```
./scripts/deploy.sh
```

It SSHs in, `git pull`s, reinstalls dependencies, rebuilds the phone-ui, and restarts the `jesbox` pm2 process. Commit and push your changes first — the script deploys whatever is on the remote's default branch.

## Known limitations (v1)

- Room codes and all state live in memory only; restarting the server clears every room (including any players sitting in the reconnect grace period).
- Microgames now has 6 rounds defined; requesting more rounds than that just repeats them. The relay protocol is generic (`type:"game"`), so adding more just means a new `MicrogameKind` in `Microgames.cs`, and a matching React component keyed by that kind in `MicrogameScreen.jsx` — no server changes needed.
- Game mode, difficulty, and round counts reset to their defaults each time Unity restarts (not persisted to disk).
- Chosen One has 5 solo minigames defined (`SoloGames.cs`); requesting more turns than the player count just cycles through them again with a new random player each time. Adding another pass/fail one means a new `SoloGameKind`, host-side render/tick logic in `GameManager.cs`, and a matching controller in `SoloTurnScreen.jsx` — still no server changes needed.
- Sketch & Guess strokes are throttled to ~25 points/sec over the network (the phone's own canvas draws every point locally, so it never looks choppy to the artist) — the TV's redraw is a little coarser than the original but plenty legible for guessing. `DrawPrompts.cs` has 14 prompts, each with 4 real Bible-scene choices (no throwaway joke decoys), so guessing takes actually reading the drawing; add more there any time.
- Bible Charades has no way to detect whether the performer actually says a forbidden word or points at things during an Act round — it's an honor-system game like the physical board games it's based on; the room self-polices.
- The QR code requires the TV to have internet access (it calls `api.qrserver.com`), which it already needs anyway for the WebSocket relay.
