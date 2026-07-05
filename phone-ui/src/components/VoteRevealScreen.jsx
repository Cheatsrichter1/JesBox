import React from 'react';

const LETTERS = ['A', 'B', 'C', 'D'];

export default function VoteRevealScreen({ game, playerId }) {
  if (!game) return null;
  const { tally = [], favoriteIndex = -1, players = [] } = game;
  const me = players.find((p) => p.id === playerId);
  const maxVotes = tally.length > 0 ? Math.max(...tally) : 0;
  const sorted = [...players].sort((a, b) => b.score - a.score);

  return (
    <div className="screen">
      <p className="result-banner">
        {favoriteIndex >= 0 ? `Crowd favorite: ${LETTERS[favoriteIndex]}` : 'No votes cast!'}
      </p>

      <div className="vote-tally">
        {tally.map((count, i) => (
          <div className="vote-tally-row" key={i}>
            <span className="choice-letter">{LETTERS[i]}</span>
            <div className="vote-tally-track">
              <div
                className="vote-tally-fill"
                style={{ width: maxVotes > 0 ? `${(count / maxVotes) * 100}%` : '0%' }}
              />
            </div>
            <span>{count}</span>
          </div>
        ))}
      </div>

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
