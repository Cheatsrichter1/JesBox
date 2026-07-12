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

function TiltController({ onSteer }) {
  const { t } = useLanguage();
  const [steer, setSteer] = useState(0);
  const [motionReady, setMotionReady] = useState(false);
  const permissionRequestedRef = useRef(false);
  const lastSendRef = useRef(0);
  const draggingRef = useRef(false);
  const trackRef = useRef(null);

  const sendThrottled = (value) => {
    const now = performance.now();
    if (now - lastSendRef.current < 40) return;
    lastSendRef.current = now;
    onSteer(value);
  };

  useEffect(() => {
    const TILT_RANGE_DEG = 30; // phone tilt at which we report full left/right

    const handleOrientation = (event) => {
      if (event.gamma == null || draggingRef.current) return;
      const normalized = Math.max(-1, Math.min(1, event.gamma / TILT_RANGE_DEG));
      setSteer(normalized);
      sendThrottled(normalized);
    };

    window.addEventListener('deviceorientation', handleOrientation);
    if (typeof DeviceOrientationEvent === 'undefined' || typeof DeviceOrientationEvent.requestPermission !== 'function') {
      setMotionReady(true);
    }

    return () => window.removeEventListener('deviceorientation', handleOrientation);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const requestMotionIfNeeded = () => {
    if (!permissionRequestedRef.current && typeof DeviceOrientationEvent !== 'undefined' &&
        typeof DeviceOrientationEvent.requestPermission === 'function') {
      permissionRequestedRef.current = true;
      DeviceOrientationEvent.requestPermission()
        .then((result) => setMotionReady(result === 'granted'))
        .catch(() => {});
    }
  };

  const updateFromPointer = (clientX) => {
    const track = trackRef.current;
    if (!track) return;
    const rect = track.getBoundingClientRect();
    const ratio = (clientX - rect.left) / rect.width;
    const normalized = Math.max(-1, Math.min(1, ratio * 2 - 1));
    setSteer(normalized);
    sendThrottled(normalized);
  };

  const handleStart = (e) => {
    draggingRef.current = true;
    requestMotionIfNeeded();
    updateFromPointer(e.touches ? e.touches[0].clientX : e.clientX);
  };
  const handleDrag = (e) => {
    if (!draggingRef.current) return;
    updateFromPointer(e.touches ? e.touches[0].clientX : e.clientX);
  };
  const handleEnd = () => {
    if (!draggingRef.current) return;
    draggingRef.current = false;
    setSteer(0);
    onSteer(0);
  };

  return (
    <div className="tilt-controller">
      <p className="tilt-hint">{motionReady ? t('solo.tiltHint') : t('solo.tiltPermission')}</p>
      <div
        className="tilt-track"
        ref={trackRef}
        onMouseDown={handleStart}
        onMouseMove={handleDrag}
        onMouseUp={handleEnd}
        onMouseLeave={handleEnd}
        onTouchStart={handleStart}
        onTouchMove={handleDrag}
        onTouchEnd={handleEnd}
      >
        <div className="tilt-center-line" />
        <div className="tilt-marker" style={{ left: `${((steer + 1) / 2) * 100}%` }}>🌊</div>
      </div>
      <div className="tilt-labels">
        <span>{t('solo.dirLeft')}</span>
        <span>{t('solo.dirRight')}</span>
      </div>
    </div>
  );
}

const CONTROLLERS = {
  FieryFurnaceDash: DirectionController,
  PartingTheSea: TiltController,
  DavidsSlingshot: FireController,
  LoavesAndFishesMultiply: FireController,
  JoyfulPrayer: ShakeController,
};

export default function SoloTurnScreen({ game, playerId, onMove, onFire, onShake, onSteer }) {
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
      {Controller && <Controller kind={game.kind} onMove={onMove} onFire={onFire} onShake={onShake} onSteer={onSteer} />}
    </div>
  );
}
