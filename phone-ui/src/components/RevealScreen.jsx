import React from 'react';

export default function RevealScreen({ game, playerId, selectedChoice }) {
  if (!game) return null;
  const { correctIndex, players = [] } = game;
  const me = players.find((p) => p.id === playerId);
  const answered = selectedChoice !== null;
  const wasCorrect = answered && selectedChoice === correctIndex;

  const sorted = [...players].sort((a, b) => b.score - a.score);

  return (
    <div className="screen">
      {!answered && <p className="result-banner">Time's up!</p>}
      {answered && (
        <p className={`result-banner ${wasCorrect ? 'correct' : 'incorrect'}`}>
          {wasCorrect ? 'Correct! ✝' : 'Not quite'}
        </p>
      )}

      {me && (
        <p className="score-line">
          {me.delta > 0 ? `+${me.delta} points — ` : ''}Total: {me.score}
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
