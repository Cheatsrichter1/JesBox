import React from 'react';

export default function LobbyScreen({ roomCode, game }) {
  const players = game?.players || [];

  return (
    <div className="screen">
      <div className="cross">✝</div>
      <h1 className="brand">You're in!</h1>
      <p className="room-badge">Room {roomCode}</p>
      <div className="spinner" />
      <p className="subtitle">Waiting for the host to start the game...</p>

      {players.length > 0 && (
        <div className="leaderboard">
          {players.map((p) => (
            <div className="leaderboard-row" key={p.id}>
              <span className="name-tag">{p.name}</span>
              <span>ready</span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
