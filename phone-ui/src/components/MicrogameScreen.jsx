import React, { useEffect, useRef, useState } from 'react';

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

export default function MicrogameScreen({ game, onTap, onSubmitScore }) {
  if (!game) return null;
  if (game.kind === 'MannaRain') return <MannaRainGame game={game} onTap={onTap} />;
  if (game.kind === 'FishersOfMen') return <FishersOfMenGame game={game} onSubmitScore={onSubmitScore} />;
  return null;
}
