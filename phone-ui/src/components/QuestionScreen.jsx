import React from 'react';
import { useLanguage } from '../i18n.jsx';

const LETTERS = ['A', 'B', 'C', 'D'];

export default function QuestionScreen({ game, selectedChoice, onAnswer }) {
  const { t } = useLanguage();
  if (!game) return null;
  const { index = 0, total = 0, question = '', choices = [], timeLimit = 8 } = game;
  const locked = selectedChoice !== null;

  return (
    <div className="screen">
      <p className="room-badge">{t('question.header', { n: index + 1, total })}</p>

      <div className="timer-track">
        <div
          key={index}
          className="timer-fill"
          style={{ animationDuration: `${timeLimit}s` }}
        />
      </div>

      <p className="question-text">{question}</p>

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

      {locked && <p className="subtitle">{t('question.locked')}</p>}
    </div>
  );
}
