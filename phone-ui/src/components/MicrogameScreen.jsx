import React, { useCallback, useEffect, useRef, useState } from 'react';

function MannaRainGame({ game, onTap }) {
  const [taps, setTaps] = useState(0);

  const handleTap = () => {
    setTaps((t) => t + 1);
    onTap();
  };

  return (
    <div className="screen">
      <p className="room-badge">Microgame {game.index + 1} / {game.total}</p>
      <div className="timer-track">
        <div key={game.index} className="timer-fill" style={{ animationDuration: `${game.duration}s` }} />
      </div>
      <p className="question-text">{game.title}</p>
      <p className="subtitle">{game.instructions}</p>
      <button className="tap-button" onClick={handleTap}>TAP!</button>
      <p className="score-line">{taps} gathered</p>
    </div>
  );
}

function FishersOfMenGame({ game, onSubmitScore }) {
  const [icons, setIcons] = useState([]);
  const [score, setScore] = useState(0);
  const scoreRef = useRef(0);
  const nextId = useRef(0);
  const submittedRef = useRef(false);

  useEffect(() => {
    scoreRef.current = 0;
    submittedRef.current = false;
    setScore(0);
    setIcons([]);

    const spawn = setInterval(() => {
      const isGood = Math.random() > 0.35;
      const id = nextId.current++;
      const icon = {
        id,
        good: isGood,
        left: 10 + Math.random() * 80,
        top: 15 + Math.random() * 65,
      };
      setIcons((cur) => [...cur, icon]);
      setTimeout(() => {
        setIcons((cur) => cur.filter((i) => i.id !== id));
      }, 900);
    }, 450);

    const endTimer = setTimeout(() => {
      clearInterval(spawn);
      setIcons([]);
      if (!submittedRef.current) {
        submittedRef.current = true;
        onSubmitScore(scoreRef.current);
      }
    }, game.duration * 1000);

    return () => {
      clearInterval(spawn);
      clearTimeout(endTimer);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [game.index]);

  const tapIcon = (icon) => {
    setIcons((cur) => cur.filter((i) => i.id !== icon.id));
    scoreRef.current += icon.good ? 1 : -1;
    setScore(scoreRef.current);
  };

  return (
    <div className="screen">
      <p className="room-badge">Microgame {game.index + 1} / {game.total}</p>
      <div className="timer-track">
        <div key={game.index} className="timer-fill" style={{ animationDuration: `${game.duration}s` }} />
      </div>
      <p className="subtitle">{game.instructions}</p>
      <div className="fishers-field">
        {icons.map((icon) => (
          <button
            key={icon.id}
            className={`fishers-icon ${icon.good ? 'good' : 'bad'}`}
            style={{ left: `${icon.left}%`, top: `${icon.top}%` }}
            onClick={() => tapIcon(icon)}
          >
            {icon.good ? '🐟' : '🔱'}
          </button>
        ))}
      </div>
      <p className="score-line">Score: {score}</p>
    </div>
  );
}

function JoyfulNoiseGame({ game, onTap }) {
  const [shakes, setShakes] = useState(0);
  const [motionReady, setMotionReady] = useState(false);
  const lastShakeRef = useRef(0);
  const lastAccelRef = useRef(null);
  const permissionRequestedRef = useRef(false);

  const registerShake = useCallback(() => {
    setShakes((s) => s + 1);
    onTap();
  }, [onTap]);

  useEffect(() => {
    const SHAKE_THRESHOLD = 12;
    const COOLDOWN_MS = 220;

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
        registerShake();
      }
    };

    window.addEventListener('devicemotion', handleMotion);
    if (typeof DeviceMotionEvent === 'undefined' || typeof DeviceMotionEvent.requestPermission !== 'function') {
      setMotionReady(true);
    }

    return () => window.removeEventListener('devicemotion', handleMotion);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [game.index]);

  const handleButtonPress = () => {
    registerShake();
    if (!permissionRequestedRef.current && typeof DeviceMotionEvent !== 'undefined' &&
        typeof DeviceMotionEvent.requestPermission === 'function') {
      permissionRequestedRef.current = true;
      DeviceMotionEvent.requestPermission()
        .then((result) => setMotionReady(result === 'granted'))
        .catch(() => {});
    }
  };

  return (
    <div className="screen">
      <p className="room-badge">Microgame {game.index + 1} / {game.total}</p>
      <div className="timer-track">
        <div key={game.index} className="timer-fill" style={{ animationDuration: `${game.duration}s` }} />
      </div>
      <p className="question-text">{game.title}</p>
      <p className="subtitle">{game.instructions}</p>
      <button className="tap-button" onClick={handleButtonPress}>
        {motionReady ? '📳 SHAKE!' : 'TAP (or shake)!'}
      </button>
      <p className="score-line">{shakes} shakes</p>
    </div>
  );
}

function WalkOnWaterGame({ game, onSubmitScore }) {
  const [playerX, setPlayerX] = useState(50);
  const [waves, setWaves] = useState([]);
  const [score, setScore] = useState(0);
  const scoreRef = useRef(0);
  const playerXRef = useRef(50);
  const nextId = useRef(0);
  const submittedRef = useRef(false);
  const areaRef = useRef(null);

  useEffect(() => {
    scoreRef.current = 0;
    playerXRef.current = 50;
    submittedRef.current = false;
    setScore(0);
    setPlayerX(50);
    setWaves([]);

    const handleOrientation = (event) => {
      if (event.gamma == null) return;
      const clamped = Math.max(-45, Math.min(45, event.gamma));
      const pct = 50 + (clamped / 45) * 45;
      playerXRef.current = pct;
      setPlayerX(pct);
    };
    window.addEventListener('deviceorientation', handleOrientation);

    const spawn = setInterval(() => {
      const id = nextId.current++;
      const left = 10 + Math.random() * 80;
      setWaves((cur) => [...cur, { id, left }]);
      setTimeout(() => {
        setWaves((cur) => {
          const wave = cur.find((w) => w.id === id);
          if (wave) {
            const dist = Math.abs(wave.left - playerXRef.current);
            scoreRef.current = dist < 12 ? Math.max(0, scoreRef.current - 1) : scoreRef.current + 1;
            setScore(scoreRef.current);
          }
          return cur.filter((w) => w.id !== id);
        });
      }, 1100);
    }, 700);

    const endTimer = setTimeout(() => {
      clearInterval(spawn);
      window.removeEventListener('deviceorientation', handleOrientation);
      setWaves([]);
      if (!submittedRef.current) {
        submittedRef.current = true;
        onSubmitScore(scoreRef.current);
      }
    }, game.duration * 1000);

    return () => {
      clearInterval(spawn);
      clearTimeout(endTimer);
      window.removeEventListener('deviceorientation', handleOrientation);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [game.index]);

  const handlePointerMove = (e) => {
    const area = areaRef.current;
    if (!area) return;
    const point = e.touches ? e.touches[0] : e;
    const rect = area.getBoundingClientRect();
    const pct = Math.max(0, Math.min(100, ((point.clientX - rect.left) / rect.width) * 100));
    playerXRef.current = pct;
    setPlayerX(pct);
  };

  return (
    <div className="screen">
      <p className="room-badge">Microgame {game.index + 1} / {game.total}</p>
      <div className="timer-track">
        <div key={game.index} className="timer-fill" style={{ animationDuration: `${game.duration}s` }} />
      </div>
      <p className="subtitle">{game.instructions}</p>
      <div
        ref={areaRef}
        className="water-field"
        onTouchMove={handlePointerMove}
        onMouseMove={handlePointerMove}
      >
        {waves.map((w) => (
          <span key={w.id} className="water-wave" style={{ left: `${w.left}%` }}>🌊</span>
        ))}
        <span className="water-player" style={{ left: `${playerX}%` }}>🧍</span>
      </div>
      <p className="score-line">Score: {score}</p>
    </div>
  );
}

function LoavesAndFishesGame({ game, onSubmitScore }) {
  const [markerPos, setMarkerPos] = useState(0);
  const [zone, setZone] = useState({ start: 40, end: 60 });
  const [score, setScore] = useState(0);
  const [flash, setFlash] = useState(null);
  const scoreRef = useRef(0);
  const posRef = useRef(0);
  const dirRef = useRef(1);
  const zoneRef = useRef({ start: 40, end: 60 });
  const submittedRef = useRef(false);

  useEffect(() => {
    scoreRef.current = 0;
    posRef.current = 0;
    dirRef.current = 1;
    zoneRef.current = { start: 40, end: 60 };
    submittedRef.current = false;
    setScore(0);
    setMarkerPos(0);
    setZone(zoneRef.current);

    const speed = 3.2;
    const tick = setInterval(() => {
      posRef.current += dirRef.current * speed;
      if (posRef.current >= 100) { posRef.current = 100; dirRef.current = -1; }
      if (posRef.current <= 0) { posRef.current = 0; dirRef.current = 1; }
      setMarkerPos(posRef.current);
    }, 30);

    const endTimer = setTimeout(() => {
      clearInterval(tick);
      if (!submittedRef.current) {
        submittedRef.current = true;
        onSubmitScore(scoreRef.current);
      }
    }, game.duration * 1000);

    return () => {
      clearInterval(tick);
      clearTimeout(endTimer);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [game.index]);

  const handleTap = () => {
    const { start, end } = zoneRef.current;
    const hit = posRef.current >= start && posRef.current <= end;
    if (hit) {
      scoreRef.current += 1;
      setFlash('hit');
      const width = 20;
      const center = 15 + Math.random() * 70;
      zoneRef.current = { start: center - width / 2, end: center + width / 2 };
      setZone(zoneRef.current);
    } else {
      scoreRef.current = Math.max(0, scoreRef.current - 1);
      setFlash('miss');
    }
    setScore(scoreRef.current);
    setTimeout(() => setFlash(null), 150);
  };

  return (
    <div className="screen">
      <p className="room-badge">Microgame {game.index + 1} / {game.total}</p>
      <div className="timer-track">
        <div key={game.index} className="timer-fill" style={{ animationDuration: `${game.duration}s` }} />
      </div>
      <p className="subtitle">{game.instructions}</p>
      <div className="loaves-track">
        <div className="loaves-zone" style={{ left: `${zone.start}%`, width: `${zone.end - zone.start}%` }} />
        <div className="loaves-marker" style={{ left: `${markerPos}%` }}>🍞</div>
      </div>
      <button
        className={`tap-button ${flash === 'hit' ? 'flash-good' : flash === 'miss' ? 'flash-bad' : ''}`}
        onClick={handleTap}
      >
        MULTIPLY!
      </button>
      <p className="score-line">Score: {score}</p>
    </div>
  );
}

function PartingWatersGame({ game, onSubmitScore }) {
  const [score, setScore] = useState(0);
  const scoreRef = useRef(0);
  const lastDirRef = useRef(null);
  const touchStartRef = useRef(null);
  const submittedRef = useRef(false);

  useEffect(() => {
    scoreRef.current = 0;
    lastDirRef.current = null;
    submittedRef.current = false;
    setScore(0);

    const endTimer = setTimeout(() => {
      if (!submittedRef.current) {
        submittedRef.current = true;
        onSubmitScore(scoreRef.current);
      }
    }, game.duration * 1000);

    return () => clearTimeout(endTimer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [game.index]);

  const handleStart = (e) => {
    const point = e.touches ? e.touches[0] : e;
    touchStartRef.current = point.clientX;
  };

  const handleEnd = (e) => {
    if (touchStartRef.current == null) return;
    const point = e.changedTouches ? e.changedTouches[0] : e;
    const deltaX = point.clientX - touchStartRef.current;
    touchStartRef.current = null;
    const THRESHOLD = 40;
    if (Math.abs(deltaX) < THRESHOLD) return;
    const dir = deltaX > 0 ? 'right' : 'left';
    if (dir !== lastDirRef.current) {
      lastDirRef.current = dir;
      scoreRef.current += 1;
      setScore(scoreRef.current);
    }
  };

  const gapPct = Math.min(90, score * 6);

  return (
    <div className="screen">
      <p className="room-badge">Microgame {game.index + 1} / {game.total}</p>
      <div className="timer-track">
        <div key={game.index} className="timer-fill" style={{ animationDuration: `${game.duration}s` }} />
      </div>
      <p className="question-text">{game.title}</p>
      <p className="subtitle">{game.instructions}</p>
      <div
        className="parting-field"
        onTouchStart={handleStart}
        onTouchEnd={handleEnd}
        onMouseDown={handleStart}
        onMouseUp={handleEnd}
      >
        <div className="parting-water left" style={{ width: `${50 - gapPct / 2}%` }} />
        <div className="parting-water right" style={{ width: `${50 - gapPct / 2}%` }} />
        <div className="parting-path" style={{ width: `${gapPct}%` }}>🚶</div>
      </div>
      <p className="score-line">{score} swipes</p>
    </div>
  );
}

const MICROGAME_COMPONENTS = {
  MannaRain: MannaRainGame,
  FishersOfMen: FishersOfMenGame,
  JoyfulNoise: JoyfulNoiseGame,
  WalkOnWater: WalkOnWaterGame,
  LoavesAndFishes: LoavesAndFishesGame,
  PartingWaters: PartingWatersGame,
};

export default function MicrogameScreen({ game, onTap, onSubmitScore }) {
  if (!game) return null;
  const Component = MICROGAME_COMPONENTS[game.kind];
  if (!Component) return null;
  return <Component game={game} onTap={onTap} onSubmitScore={onSubmitScore} />;
}
