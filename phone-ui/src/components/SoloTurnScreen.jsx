import React from 'react';

function FieryFurnaceDashController({ onMove }) {
  return (
    <div className="solo-controller-row">
      <button className="solo-dir-btn" onClick={() => onMove(-1)}>◀ LEFT</button>
      <button className="solo-dir-btn" onClick={() => onMove(1)}>RIGHT ▶</button>
    </div>
  );
}

function DavidsSlingshotController({ onFire }) {
  return (
    <button className="tap-button solo-fire-btn" onClick={onFire}>🔥 FIRE!</button>
  );
}

const CONTROLLERS = {
  FieryFurnaceDash: FieryFurnaceDashController,
  DavidsSlingshot: DavidsSlingshotController,
};

export default function SoloTurnScreen({ game, playerId, onMove, onFire }) {
  if (!game) return null;
  const isMe = game.chosenId === playerId;

  if (!isMe) {
    return (
      <div className="screen">
        <p className="room-badge">Chosen One — Turn {game.index + 1} / {game.total}</p>
        <div className="cross">✝</div>
        <p className="question-text">{game.chosenName}'s turn!</p>
        <p className="subtitle">Look up at the screen and cheer them on — you're up soon!</p>
      </div>
    );
  }

  const Controller = CONTROLLERS[game.kind];

  return (
    <div className="screen">
      <p className="room-badge">Chosen One — Turn {game.index + 1} / {game.total}</p>
      <div className="timer-track">
        <div key={game.index} className="timer-fill" style={{ animationDuration: `${game.duration}s` }} />
      </div>
      <p className="question-text">{game.title}</p>
      <p className="subtitle">{game.controllerInstructions}</p>
      <p className="solo-watch-hint">👀 Watch the big screen!</p>
      {Controller && <Controller onMove={onMove} onFire={onFire} />}
    </div>
  );
}
