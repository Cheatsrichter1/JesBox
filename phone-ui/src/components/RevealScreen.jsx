import React from 'react';
import { useLanguage } from '../i18n.jsx';

export default function RevealScreen({ game, playerId, selectedChoice }) {
  const { t } = useLanguage();
  if (!game) return null;
  const { correctIndex, players = [] } = game;
  const me = players.find((p) => p.id === playerId);
  const answered = selectedChoice !== null;
  const wasCorrect = answered && selectedChoice === correctIndex;

  const sorted = [...players].sort((a, b) => b.score - a.score);

  return (
    <div className="screen">
      {!answered && <p className="result-banner">{t('reveal.timeUp')}</p>}
      {answered && (
        <p className={`result-banner ${wasCorrect ? 'correct' : 'incorrect'}`}>
          {wasCorrect ? t('reveal.correct') : t('reveal.notQuite')}
        </p>
      )}

      {me && (
        <p className="score-line">
          {me.delta > 0 ? t('reveal.pointsPrefix', { delta: me.delta }) : ''}{t('reveal.total', { score: me.score })}
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
