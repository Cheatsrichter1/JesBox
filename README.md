# JesBox

A Jackbox/Kahoot-style party game with a Christian trivia theme:
- **Unity** is the "TV" — one big screen that everyone in the room watches. It hosts the room, runs the quiz, and shows the scoreboard.
- **React (phone-ui)** is the controller — each player opens a web page on their own phone, joins with a 4-letter room code, and taps answers.
- **Node server** is the relay in between — it hands out room codes and forwards messages between the Unity host and every player's phone. It also serves the built phone-ui as static files, so the whole thing is one process to deploy.

This first version ships one game mode: a fast-paced Bible trivia buzzer round (10 questions in the bank, 5 played per game, speed-bonus scoring, live scoreboard).

## Repo layout

```
Assets/Scripts/Net/     Unity WebSocket client + message contracts
Assets/Scripts/Game/    Unity GameManager (state machine) + trivia question bank
Assets/Scripts/UI/      Unity runtime UI builder (no manual scene wiring needed)
server/                 Node.js relay server + static host for phone-ui
phone-ui/               React (Vite) phone controller app
```

## How the pieces talk to each other

Everything is plain JSON over one WebSocket per client, relayed by the server:

- Unity connects first and sends `{"type":"create_room"}` → server replies with a room code.
- Each phone connects and sends `{"type":"join","roomCode":"ABCD","name":"..."}` → server assigns a `playerId` and tells the host.
- From then on, both sides just send `{"type":"game","data":{...}}`. The server doesn't understand `data` at all — it just relays host→all-players or player→host. All game rules (questions, timing, scoring) live in Unity; the phones are dumb display+input surfaces.

See `server/index.js` for the exact relay rules and `Assets/Scripts/Net/Messages.cs` for the payload shapes used by the trivia round (`lobby` / `question` / `reveal` / `final` phases).

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

3. In Unity, set **Server Url** on the `GameManager` to your real address, e.g. `wss://jesbox.yourdomain.com/ws` (use `wss://` once your server has HTTPS, which most reverse proxies/hosting will require for phones to load the page over `https://` in the first place).

4. Build the Unity project as a standalone build (File → Build Profiles → Windows/Mac, Build) — that's the executable you run on the TV/host machine. It does not need to be hosted; it just needs network access to your server.

## Deploying to your server

This assumes a server where you can run a persistent Node.js process (a VPS, Docker host, etc.).

1. Copy the `server/` folder and the built `phone-ui/dist` folder to the server (keep the relative layout: `.../server/index.js` and `.../phone-ui/dist/...` as siblings, matching this repo).
2. On the server:
   ```
   cd server
   npm install --omit=dev
   PORT=8080 npm start
   ```
   Keep it running with a process manager, e.g. `pm2 start index.js --name jesbox` or a systemd unit.
3. Put a reverse proxy (nginx/Caddy) in front with TLS, forwarding both normal HTTP requests and the `/ws` WebSocket upgrade to port 8080. Caddy does this with zero config for WebSockets; for nginx you need `proxy_set_header Upgrade $http_upgrade;` and `Connection "upgrade"` on the `/ws` location.
4. Point players at `https://your-domain/` — that's the phone join page. Point the Unity build's **Server Url** field at `wss://your-domain/ws`.

## Known limitations (v1)

- No reconnect handling — if a phone loses the WebSocket mid-game it has to rejoin as a new player.
- Room codes and all state live in memory only; restarting the server clears every room.
- One game mode (trivia buzzer). The relay protocol is generic (`type:"game"`), so adding a WarioWare-style reflex minigame later just means defining new `data.phase` payloads and a new React screen — no server changes needed.
- No QR code on the lobby screen yet, just the room code + URL as text.
