import React from 'react';

const LETTERS = ['A', 'B', 'C', 'D'];

export default function VotePromptScreen({ game, selectedChoice, onVote }) {
  if (!game) return null;
  const { index = 0, total = 0, scenario = '', options = [], timeLimit = 12 } = game;
  const locked = selectedChoice !== null;

  return (
    <div className="screen">
      <p className="room-badge">Vote! Prompt {index + 1} of {total}</p>

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

      {locked && <p className="subtitle">Vote locked in — waiting for others...</p>}
    </div>
  );
}
