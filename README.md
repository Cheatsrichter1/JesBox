# JesBox

A Jackbox/Kahoot-style party game with a Christian trivia theme:
- **Unity** is the "TV" — one big screen that everyone in the room watches. It hosts the room, runs the quiz, and shows the scoreboard.
- **React (phone-ui)** is the controller — each player opens a web page on their own phone, joins with a 4-letter room code, and taps answers.
- **Node server** is the relay in between — it hands out room codes and forwards messages between the Unity host and every player's phone. It also serves the built phone-ui as static files, so the whole thing is one process to deploy.

The host (TV/Unity) picks one of five game modes from the lobby screen before hitting Start:

- **Trivia Quiz** — Bible trivia buzzer round. Selectable difficulty (Easy/Medium/Hard, ~8 questions each), question count (3–10), and per-question time limit (5–30s), all adjustable with +/- steppers in the lobby. Speed-bonus scoring, live scoreboard.
- **Microgames** — WarioWare-style rapid-fire rounds (2–8 rounds, picked at random from the bank) that everyone plays at once on their own phone: *Manna Rain* (mash-to-tap race), *Fishers of Men* (tap the fish, avoid the tridents), *Joyful Noise* (shake your phone like a tambourine, using the accelerometer with a tap fallback), *Walk on Water* (tilt or drag to dodge falling waves), *Loaves and Fishes* (tap when the sweeping marker hits the target zone), and *Parting the Waters* (swipe alternating left/right). All but Manna Rain and Joyful Noise run their reflex gameplay client-side and submit a final score.
- **Prompt & Vote** — Quiplash-style multiple-choice scenario voting (2–8 prompts). Everyone votes for one of 4 preset responses to a scenario; whoever matches the crowd favorite gets the bonus, everyone else gets a small participation score.
- **Chosen One** — true WarioWare-style solo spotlight (2–12 turns, host-adjustable). Each turn a random player (never repeating until everyone's had a turn) is handed a phone controller — everyone else's phone just shows who's up — while the actual minigame renders live on the TV. Every game is a quick (3–6s) single pass/fail challenge, not a scored timer: *Fiery Furnace Dash* (dodge 3 falling flames across 3 lanes, one hit and you're out), *David's Slingshot* (one shot to hit a swinging Goliath), *Joyful Prayer* (shake the phone fast enough to fill the meter in time), *Loaves and Fishes* (one tap to catch a sweeping target in its zone), and *Parting the Sea* (alternate LEFT/RIGHT taps fast enough to hit the target count). A win pays 500 points, a fail pays 0 — no partial credit. Gameplay state (positions, hits, meters) lives entirely in `GameManager`, driven in real time by `move`/`fire`/`shake` messages from the one chosen phone.
- **Sketch & Guess** — a separate mode (2–10 rounds, host-adjustable), same fair-rotation picking as Chosen One but its own two-phase flow: the chosen player draws a secret Bible-themed prompt on their phone (strokes stream to the TV and render live as ink segments), then everyone else picks the answer from 4 options. Correct guessers get 500 each, and the artist gets a 150-point bonus per correct guesser.

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
- There's one more message shape, `{"type":"game_to","playerId":"p3","data":{...}}`, host-only, relayed to exactly one player instead of the whole room. It exists for Sketch & Guess's secret answer — the only case where a broadcast would leak information to the wrong player.

See `server/index.js` for the exact relay rules and `Assets/Scripts/Net/Messages.cs` for every payload shape, keyed by `phase`: `lobby`, `question`/`reveal` (trivia), `microgame`/`microgame_reveal` (microgames), `vote_prompt`/`vote_reveal` (prompt & vote), `solo_turn`/`solo_reveal` (Chosen One), `sketch_draw`/`sketch_answer`/`sketch_guess` (Sketch & Guess, which reuses `solo_reveal` for its own reveal), and `final`. Phone→host input is a single generic shape, `{"action": "answer"|"vote"|"tap"|"submit_score"|"move"|"fire"|"shake"|"draw_point"|"draw_clear", "choice": n, "value": n, "x": n, "y": n, "newStroke": bool}` — for Chosen One and Sketch & Guess, only the currently-chosen/drawing player's phone sends `move`/`fire`/`shake`/`draw_point`/`draw_clear`, and the host ignores those actions from anyone else. Sketch guesses reuse the plain `answer` action.

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

- No reconnect handling — if a phone loses the WebSocket mid-game it has to rejoin as a new player.
- Room codes and all state live in memory only; restarting the server clears every room.
- Microgames now has 6 rounds defined; requesting more rounds than that just repeats them. The relay protocol is generic (`type:"game"`), so adding more just means a new `MicrogameKind` in `Microgames.cs`, and a matching React component keyed by that kind in `MicrogameScreen.jsx` — no server changes needed.
- No QR code on the lobby screen yet, just the room code + URL as text.
- Game mode, difficulty, and round counts reset to their defaults each time Unity restarts (not persisted to disk).
- Chosen One has 5 solo minigames defined (`SoloGames.cs`); requesting more turns than the player count just cycles through them again with a new random player each time. Adding another pass/fail one means a new `SoloGameKind`, host-side render/tick logic in `GameManager.cs`, and a matching controller in `SoloTurnScreen.jsx` — still no server changes needed.
- Sketch & Guess strokes are throttled to ~25 points/sec over the network (the phone's own canvas draws every point locally, so it never looks choppy to the artist) — the TV's redraw is a little coarser than the original but plenty legible for guessing. `DrawPrompts.cs` has 12 prompts; add more there any time.
