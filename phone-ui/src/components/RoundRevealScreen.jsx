import React from 'react';

export default function RoundRevealScreen({ game, playerId }) {
  if (!game) return null;
  const players = game.players || [];
  const me = players.find((p) => p.id === playerId);
  const sorted = [...players].sort((a, b) => b.score - a.score);

  return (
    <div className="screen">
      <p className="result-banner">{game.title || 'Round Results!'}</p>

      {me && (
        <p className="score-line">
          {me.delta > 0 ? `+${me.delta} points — ` : 'No points this round — '}Total: {me.score}
        </p>
      )}

      <div className="leaderboard">
        {sorted.slice(0, 5).map((p, i) => (
          <div className={`leaderboard-row${p.id === playerId ? ' me' : ''}`} key={p.id}>
            <span><span className="leaderboard-rank">{i + 1}.</span>{p.name}</span>
            <span>{p.score}</span>
          </div>
        ))}
      </div>
    </div>
  );
}
