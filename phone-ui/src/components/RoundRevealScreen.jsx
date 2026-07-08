import React from 'react';
import { useLanguage } from '../i18n.jsx';

export default function RoundRevealScreen({ game, playerId }) {
  const { t } = useLanguage();
  if (!game) return null;
  const players = game.players || [];
  const me = players.find((p) => p.id === playerId);
  const sorted = [...players].sort((a, b) => b.score - a.score);

  return (
    <div className="screen">
      <p className="result-banner">{game.title || t('roundReveal.defaultTitle')}</p>

      {me && (
        <p className="score-line">
          {me.delta > 0 ? t('reveal.pointsPrefix', { delta: me.delta }) : t('roundReveal.noPointsPrefix')}{t('reveal.total', { score: me.score })}
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
