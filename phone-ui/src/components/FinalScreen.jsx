import React from 'react';
import { useLanguage } from '../i18n.jsx';

export default function FinalScreen({ game, playerId }) {
  const { t } = useLanguage();
  const players = game?.players || [];
  const sorted = [...players].sort((a, b) => b.score - a.score);
  const myRank = sorted.findIndex((p) => p.id === playerId);

  return (
    <div className="screen">
      <div className="cross">✝</div>
      <h1 className="brand">{t('final.title')}</h1>
      {myRank === 0 && <p className="subtitle">{t('final.ledFlock')}</p>}

      <div className="leaderboard">
        {sorted.map((p, i) => (
          <div className={`leaderboard-row${p.id === playerId ? ' me' : ''}`} key={p.id}>
            <span><span className="leaderboard-rank">{i + 1}.</span>{p.name}</span>
            <span>{p.score}</span>
          </div>
        ))}
      </div>

      <p className="subtitle">{t('final.thanks')}</p>
    </div>
  );
}
