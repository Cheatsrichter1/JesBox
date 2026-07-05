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

/** roomCode -> { hostWs, players: Map<playerId, { ws, name }> } */
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
        room.players.set(playerId, { ws, name });
        ws.role = 'player';
        ws.roomCode = roomCode;
        ws.playerId = playerId;
        send(ws, { type: 'joined', playerId, roomCode });
        send(room.hostWs, { type: 'player_joined', playerId, name });
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
      room.players.delete(ws.playerId);
      send(room.hostWs, { type: 'player_left', playerId: ws.playerId });
    }
  });
});

server.listen(PORT, () => {
  console.log(`JesBox server listening on :${PORT}`);
});
