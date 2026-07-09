import React from 'react';
import { useLanguage } from '../i18n.jsx';

export default function CharadeTurnScreen({ game, playerId, secret }) {
  const { t } = useLanguage();
  if (!game) return null;
  const isMe = game.chosenId === playerId;
  const isAct = game.charadeType === 'act';

  if (!isMe) {
    return (
      <div className="screen">
        <p className="room-badge">{t('charade.roundHeader', { n: game.index + 1, total: game.total })}</p>
        <div className="cross">✝</div>
        <p className="question-text">{t('charade.isPerformingWatchers', { name: game.chosenName })}</p>
        <p className="subtitle">{t('charade.watchAndGuess')}</p>
      </div>
    );
  }

  return (
    <div className="screen">
      <p className="room-badge">{t('charade.roundHeader', { n: game.index + 1, total: game.total })}</p>
      <div className="timer-track">
        <div key={game.index} className="timer-fill" style={{ animationDuration: `${game.duration}s` }} />
      </div>
      <p className="question-text">
        {isAct
          ? t('charade.actLabel', { prompt: secret?.prompt || '...' })
          : t('charade.describeLabel', { prompt: secret?.prompt || '...' })}
      </p>
      {!isAct && secret?.forbidden?.length > 0 && (
        <p className="subtitle">{t('charade.forbiddenWords', { words: secret.forbidden.join(', ') })}</p>
      )}
      <p className="subtitle">{isAct ? t('charade.actInstructionsPhone') : t('charade.describeInstructionsPhone')}</p>
    </div>
  );
}
