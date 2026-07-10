// JesBox relay server.
//
// Responsibilities:
//   - Serve the built phone-ui React app as static files.
//   - Run a WebSocket relay at /ws that connects one Unity "host" (the TV/big
//     screen) to many phone "players" per room.
//   - Own room codes and player identity. Everything else (game rules,
//     scoring, question content) lives in Unity — this server only relays
//     opaque "game" messages between the host and its players.
const path = require('path');
const http = require('http');
const express = require('express');
const { WebSocketServer } = require('ws');

const PORT = process.env.PORT || 8080;
const ROOM_CODE_CHARS = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789'; // no 0/O/1/I
const ROOM_CODE_LENGTH = 4;
// How long a disconnected player's slot (identity + score, which lives in
// Unity, not here) is held open for a `rejoin` before it's given up for good.
const RECONNECT_GRACE_MS = 60_000;

const app = express();
const distPath = path.join(__dirname, '..', 'phone-ui', 'dist');
app.use(express.static(distPath));
app.get('/healthz', (req, res) => res.send('ok'));
// SPA fallback for anything else (but not the /ws upgrade path).
app.get(/^\/(?!ws).*/, (req, res) => {
  res.sendFile(path.join(distPath, 'index.html'));
});

const server = http.createServer(app);
const wss = new WebSocketServer({ server, path: '/ws' });

/** roomCode -> { hostWs, players: Map<playerId, { ws, name, disconnectTimer }> } */
const rooms = new Map();
let nextPlayerNum = 1;

function makeRoomCode() {
  let code;
  do {
    code = '';
    for (let i = 0; i < ROOM_CODE_LENGTH; i++) {
      code += ROOM_CODE_CHARS[Math.floor(Math.random() * ROOM_CODE_CHARS.length)];
    }
  } while (rooms.has(code));
  return code;
}

function send(ws, obj) {
  if (ws && ws.readyState === ws.OPEN) ws.send(JSON.stringify(obj));
}

function broadcastToPlayers(room, obj) {
  for (const { ws } of room.players.values()) send(ws, obj);
}

wss.on('connection', (ws) => {
  ws.role = null;
  ws.roomCode = null;
  ws.playerId = null;

  ws.on('message', (raw) => {
    let msg;
    try {
      msg = JSON.parse(raw);
    } catch {
      return; // ignore malformed input
    }

    switch (msg.type) {
      case 'create_room': {
        const roomCode = makeRoomCode();
        rooms.set(roomCode, { hostWs: ws, players: new Map() });
        ws.role = 'host';
        ws.roomCode = roomCode;
        send(ws, { type: 'room_created', roomCode });
        break;
      }

      case 'join': {
        const roomCode = String(msg.roomCode || '').toUpperCase();
        const name = String(msg.name || '').slice(0, 20).trim() || 'Player';
        const room = rooms.get(roomCode);
        if (!room) {
          send(ws, { type: 'join_error', message: 'Room not found. Check the code.' });
          return;
        }
        const playerId = `p${nextPlayerNum++}`;
        room.players.set(playerId, { ws, name, disconnectTimer: null });
        ws.role = 'player';
        ws.roomCode = roomCode;
        ws.playerId = playerId;
        send(ws, { type: 'joined', playerId, roomCode });
        send(room.hostWs, { type: 'player_joined', playerId, name });
        break;
      }

      case 'rejoin': {
        // A phone that dropped mid-game reclaiming its old identity (and
        // therefore its score, which Unity keeps keyed by playerId) instead
        // of joining as a brand-new player. Only works within the grace
        // window below — after that the slot's been given up for good.
        const roomCode = String(msg.roomCode || '').toUpperCase();
        const playerId = String(msg.playerId || '');
        const room = rooms.get(roomCode);
        const player = room && room.players.get(playerId);
        if (!room || !player) {
          send(ws, { type: 'join_error', message: 'Session expired — please rejoin.' });
          return;
        }
        if (player.disconnectTimer) {
          clearTimeout(player.disconnectTimer);
          player.disconnectTimer = null;
        }
        player.ws = ws;
        ws.role = 'player';
        ws.roomCode = roomCode;
        ws.playerId = playerId;
        send(ws, { type: 'joined', playerId, roomCode });
        send(room.hostWs, { type: 'player_reconnected', playerId });
        break;
      }

      case 'game': {
        const room = rooms.get(ws.roomCode);
        if (!room) return;
        if (ws.role === 'host') {
          broadcastToPlayers(room, { type: 'game', data: msg.data });
        } else if (ws.role === 'player') {
          const player = room.players.get(ws.playerId);
          send(room.hostWs, {
            type: 'game',
            playerId: ws.playerId,
            name: player ? player.name : null,
            data: msg.data,
          });
        }
        break;
      }

      case 'game_to': {
        // Host-only: relay to exactly one player (e.g. a secret only that
        // player should see), instead of the whole room.
        const room = rooms.get(ws.roomCode);
        if (!room || ws.role !== 'host') return;
        const target = room.players.get(msg.playerId);
        if (target) send(target.ws, { type: 'game', data: msg.data });
        break;
      }

      default:
        break; // ignore unknown message types
    }
  });

  ws.on('close', () => {
    const room = rooms.get(ws.roomCode);
    if (!room) return;

    if (ws.role === 'host') {
      broadcastToPlayers(room, { type: 'host_left' });
      rooms.delete(ws.roomCode);
    } else if (ws.role === 'player') {
      const player = room.players.get(ws.playerId);
      // If a rejoin already replaced this player's socket, this is just the
      // old socket's close event arriving late — ignore it.
      if (!player || player.ws !== ws) return;

      player.ws = null;
      send(room.hostWs, { type: 'player_disconnected', playerId: ws.playerId });
      player.disconnectTimer = setTimeout(() => {
        room.players.delete(ws.playerId);
        send(room.hostWs, { type: 'player_left', playerId: ws.playerId });
      }, RECONNECT_GRACE_MS);
    }
  });
});

server.listen(PORT, () => {
  console.log(`JesBox server listening on :${PORT}`);
});
