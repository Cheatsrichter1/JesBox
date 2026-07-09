import React, { useEffect, useRef, useState } from 'react';
import { useLanguage } from '../i18n.jsx';

function DirectionController({ onMove }) {
  const { t } = useLanguage();
  return (
    <div className="solo-controller-row">
      <button className="solo-dir-btn" onClick={() => onMove(-1)}>{t('solo.dirLeft')}</button>
      <button className="solo-dir-btn" onClick={() => onMove(1)}>{t('solo.dirRight')}</button>
    </div>
  );
}

function FireController({ kind, onFire }) {
  const { t } = useLanguage();
  const fireLabels = {
    DavidsSlingshot: t('solo.fire'),
    LoavesAndFishesMultiply: t('solo.multiply'),
  };
  return (
    <button className="tap-button solo-fire-btn" onClick={onFire}>{fireLabels[kind] || t('solo.go')}</button>
  );
}

function ShakeController({ onShake }) {
  const { t } = useLanguage();
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
      {motionReady ? t('solo.shakeReady') : t('solo.shakeNotReady')}
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
  const { t } = useLanguage();
  if (!game) return null;
  const isMe = game.chosenId === playerId;

  if (!isMe) {
    return (
      <div className="screen">
        <p className="room-badge">{t('solo.header', { n: game.index + 1, total: game.total })}</p>
        <div className="cross">✝</div>
        <p className="question-text">{t('solo.turnAnnounce', { name: game.chosenName })}</p>
        <p className="subtitle">{t('solo.watchCheer')}</p>
      </div>
    );
  }

  const Controller = CONTROLLERS[game.kind];

  return (
    <div className="screen">
      <p className="room-badge">{t('solo.header', { n: game.index + 1, total: game.total })}</p>
      <div className="timer-track">
        <div key={game.index} className="timer-fill" style={{ animationDuration: `${game.duration}s` }} />
      </div>
      {game.verb && <p key={game.index} className="solo-verb-pop">{game.verb}</p>}
      <p className="question-text">{game.title}</p>
      <p className="subtitle">{game.controllerInstructions}</p>
      <p className="solo-watch-hint">{t('solo.watchHint')}</p>
      {Controller && <Controller kind={game.kind} onMove={onMove} onFire={onFire} onShake={onShake} />}
    </div>
  );
}
