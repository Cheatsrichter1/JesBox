import React from 'react';
import { useLanguage } from '../i18n.jsx';

const LETTERS = ['A', 'B', 'C', 'D'];

export default function VotePromptScreen({ game, selectedChoice, onVote }) {
  const { t } = useLanguage();
  if (!game) return null;
  const { index = 0, total = 0, scenario = '', options = [], timeLimit = 12 } = game;
  const locked = selectedChoice !== null;

  return (
    <div className="screen">
      <p className="room-badge">{t('vote.header', { n: index + 1, total })}</p>

      <div className="timer-track">
        <div key={index} className="timer-fill" style={{ animationDuration: `${timeLimit}s` }} />
      </div>

      <p className="question-text">{scenario}</p>

      <div className="choices">
        {options.map((opt, i) => (
          <button
            key={i}
            className={`choice-btn${selectedChoice === i ? ' selected' : ''}`}
            disabled={locked}
            onClick={() => onVote(i)}
          >
            <span className="choice-letter">{LETTERS[i]}</span>
            {opt}
          </button>
        ))}
      </div>

      {locked && <p className="subtitle">{t('vote.locked')}</p>}
    </div>
  );
}
