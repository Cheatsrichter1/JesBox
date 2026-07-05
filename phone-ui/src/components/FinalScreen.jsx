import React from 'react';

export default function FinalScreen({ game, playerId }) {
  const players = game?.players || [];
  const sorted = [...players].sort((a, b) => b.score - a.score);
  const myRank = sorted.findIndex((p) => p.id === playerId);

  return (
    <div className="screen">
      <div className="cross">✝</div>
      <h1 className="brand">Final Scores</h1>
      {myRank === 0 && <p className="subtitle">You led the flock! 🎉</p>}

      <div className="leaderboard">
        {sorted.map((p, i) => (
          <div className={`leaderboard-row${p.id === playerId ? ' me' : ''}`} key={p.id}>
            <span><span className="leaderboard-rank">{i + 1}.</span>{p.name}</span>
            <span>{p.score}</span>
          </div>
        ))}
      </div>

      <p className="subtitle">Thanks for playing — ask the host to start a new game.</p>
    </div>
  );
}
