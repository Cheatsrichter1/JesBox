import React, { useEffect, useRef, useState } from 'react';

function DirectionController({ onMove }) {
  return (
    <div className="solo-controller-row">
      <button className="solo-dir-btn" onClick={() => onMove(-1)}>◀ LEFT</button>
      <button className="solo-dir-btn" onClick={() => onMove(1)}>RIGHT ▶</button>
    </div>
  );
}

const FIRE_LABELS = {
  DavidsSlingshot: '🔥 FIRE!',
  LoavesAndFishesMultiply: '🍞 MULTIPLY!',
};

function FireController({ kind, onFire }) {
  return (
    <button className="tap-button solo-fire-btn" onClick={onFire}>{FIRE_LABELS[kind] || 'GO!'}</button>
  );
}

function ShakeController({ onShake }) {
  const [motionReady, setMotionReady] = useState(false);
  const lastShakeRef = useRef(0);
  const lastAccelRef = useRef(null);
  const permissionRequestedRef = useRef(false);

  useEffect(() => {
    const SHAKE_THRESHOLD = 12;
    const COOLDOWN_MS = 90;

    const handleMotion = (event) => {
      const acc = event.accelerationIncludingGravity || event.acceleration;
      if (!acc || acc.x == null) return;
      const { x, y, z } = acc;
      const last = lastAccelRef.current;
      lastAccelRef.current = { x, y, z };
      if (!last) return;
      const delta = Math.abs(x - last.x) + Math.abs(y - last.y) + Math.abs(z - last.z);
      const now = Date.now();
      if (delta > SHAKE_THRESHOLD && now - lastShakeRef.current > COOLDOWN_MS) {
        lastShakeRef.current = now;
        onShake();
      }
    };

    window.addEventListener('devicemotion', handleMotion);
    if (typeof DeviceMotionEvent === 'undefined' || typeof DeviceMotionEvent.requestPermission !== 'function') {
      setMotionReady(true);
    }

    return () => window.removeEventListener('devicemotion', handleMotion);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handlePress = () => {
    onShake();
    if (!permissionRequestedRef.current && typeof DeviceMotionEvent !== 'undefined' &&
        typeof DeviceMotionEvent.requestPermission === 'function') {
      permissionRequestedRef.current = true;
      DeviceMotionEvent.requestPermission()
        .then((result) => setMotionReady(result === 'granted'))
        .catch(() => {});
    }
  };

  return (
    <button className="tap-button solo-fire-btn" onClick={handlePress}>
      {motionReady ? '📳 SHAKE!' : '🙏 PRAY (tap or shake)!'}
    </button>
  );
}

const CONTROLLERS = {
  FieryFurnaceDash: DirectionController,
  PartingTheSea: DirectionController,
  DavidsSlingshot: FireController,
  LoavesAndFishesMultiply: FireController,
  JoyfulPrayer: ShakeController,
};

export default function SoloTurnScreen({ game, playerId, onMove, onFire, onShake }) {
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
      {Controller && <Controller kind={game.kind} onMove={onMove} onFire={onFire} onShake={onShake} />}
    </div>
  );
}
