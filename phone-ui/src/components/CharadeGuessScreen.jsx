import React from 'react';
import { useLanguage } from '../i18n.jsx';

const LETTERS = ['A', 'B', 'C', 'D'];

export default function CharadeGuessScreen({ game, playerId, selectedChoice, onAnswer }) {
  const { t } = useLanguage();
  if (!game) return null;
  const isPerformer = game.chosenId === playerId;
  const { index = 0, total = 0, chosenName = '', choices = [], timeLimit = 10 } = game;

  if (isPerformer) {
    return (
      <div className="screen">
        <p className="room-badge">{t('charade.roundHeader', { n: index + 1, total })}</p>
        <div className="spinner" />
        <p className="question-text">{t('charade.waitingGuesses')}</p>
      </div>
    );
  }

  const locked = selectedChoice !== null;

  return (
    <div className="screen">
      <p className="room-badge">{t('charade.roundHeader', { n: index + 1, total })}</p>
      <div className="timer-track">
        <div key={index} className="timer-fill" style={{ animationDuration: `${timeLimit}s` }} />
      </div>
      <p className="question-text">{t('charade.whatWasPerformed', { name: chosenName })}</p>
      <div className="choices">
        {choices.map((choice, i) => (
          <button
            key={i}
            className={`choice-btn${selectedChoice === i ? ' selected' : ''}`}
            disabled={locked}
            onClick={() => onAnswer(i)}
          >
            <span className="choice-letter">{LETTERS[i]}</span>
            {choice}
          </button>
        ))}
      </div>
      {locked && <p className="subtitle">{t('charade.guessLocked')}</p>}
    </div>
  );
}
