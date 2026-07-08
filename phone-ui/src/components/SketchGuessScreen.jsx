import React from 'react';

const LETTERS = ['A', 'B', 'C', 'D'];

export default function SketchGuessScreen({ game, playerId, selectedChoice, onAnswer }) {
  if (!game) return null;
  const isArtist = game.chosenId === playerId;
  const { index = 0, total = 0, chosenName = '', choices = [], timeLimit = 10 } = game;

  if (isArtist) {
    return (
      <div className="screen">
        <p className="room-badge">Sketch & Guess — Round {index + 1} / {total}</p>
        <div className="spinner" />
        <p className="question-text">Waiting for everyone to guess your drawing...</p>
      </div>
    );
  }

  const locked = selectedChoice !== null;

  return (
    <div className="screen">
      <p className="room-badge">Chosen One — Turn {index + 1} / {total}</p>
      <div className="timer-track">
        <div key={index} className="timer-fill" style={{ animationDuration: `${timeLimit}s` }} />
      </div>
      <p className="question-text">What did {chosenName} draw?</p>
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
      {locked && <p className="subtitle">Guess locked in — waiting for others...</p>}
    </div>
  );
}
