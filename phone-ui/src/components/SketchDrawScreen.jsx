import React, { useEffect, useRef, useState } from 'react';

const SEND_INTERVAL_MS = 40; // throttles network sends; local canvas still draws every event

// Indices must match SketchPalette in GameManager.cs.
const PALETTE = [
  '#221208', // black
  '#c0392b', // red
  '#e07b39', // orange
  '#c9a227', // gold
  '#2e8b57', // green
  '#2980b9', // blue
  '#8e44ad', // purple
];

// Indices must match SketchBrushSizes in GameManager.cs.
const BRUSH_SIZES = [3, 6, 10];
const BRUSH_LABELS = ['S', 'M', 'L'];

function ArtistCanvas({ duration, roundIndex, secretAnswer, onDrawPoint, onDrawClear }) {
  const canvasRef = useRef(null);
  const drawingRef = useRef(false);
  const lastPointRef = useRef(null);
  const lastSendRef = useRef(0);
  const [colorIndex, setColorIndex] = useState(0);
  const [brushIndex, setBrushIndex] = useState(1);
  const colorIndexRef = useRef(0);
  const brushIndexRef = useRef(1);

  useEffect(() => { colorIndexRef.current = colorIndex; }, [colorIndex]);
  useEffect(() => { brushIndexRef.current = brushIndex; }, [brushIndex]);

  useEffect(() => {
    const canvas = canvasRef.current;
    const ctx = canvas.getContext('2d');
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.lineCap = 'round';
    ctx.lineJoin = 'round';
  }, [roundIndex]);

  const pointFromEvent = (e) => {
    const canvas = canvasRef.current;
    const rect = canvas.getBoundingClientRect();
    const point = e.touches ? e.touches[0] : e;
    const x = (point.clientX - rect.left) / rect.width;
    const y = (point.clientY - rect.top) / rect.height;
    return { x: Math.min(1, Math.max(0, x)), y: Math.min(1, Math.max(0, y)) };
  };

  const drawLocalSegment = (from, to) => {
    const canvas = canvasRef.current;
    const ctx = canvas.getContext('2d');
    ctx.strokeStyle = PALETTE[colorIndexRef.current];
    ctx.lineWidth = BRUSH_SIZES[brushIndexRef.current];
    ctx.beginPath();
    ctx.moveTo(from.x * canvas.width, from.y * canvas.height);
    ctx.lineTo(to.x * canvas.width, to.y * canvas.height);
    ctx.stroke();
  };

  const handleStart = (e) => {
    e.preventDefault();
    drawingRef.current = true;
    const p = pointFromEvent(e);
    lastPointRef.current = p;
    onDrawPoint(p.x, p.y, true, colorIndexRef.current, brushIndexRef.current);
  };

  const handleMove = (e) => {
    if (!drawingRef.current) return;
    e.preventDefault();
    const p = pointFromEvent(e);
    const last = lastPointRef.current;
    if (last) drawLocalSegment(last, p);
    lastPointRef.current = p;

    const now = performance.now();
    if (now - lastSendRef.current >= SEND_INTERVAL_MS) {
      lastSendRef.current = now;
      onDrawPoint(p.x, p.y, false, colorIndexRef.current, brushIndexRef.current);
    }
  };

  const handleEnd = () => {
    drawingRef.current = false;
    lastPointRef.current = null;
  };

  const handleClear = () => {
    const canvas = canvasRef.current;
    canvas.getContext('2d').clearRect(0, 0, canvas.width, canvas.height);
    onDrawClear();
  };

  return (
    <div className="screen">
      <p className="room-badge">Sketch That Verse</p>
      <div className="timer-track">
        <div key={roundIndex} className="timer-fill" style={{ animationDuration: `${duration}s` }} />
      </div>
      <p className="question-text">Draw: {secretAnswer || '...'}</p>
      <canvas
        ref={canvasRef}
        className="sketch-canvas"
        width={450}
        height={210}
        onMouseDown={handleStart}
        onMouseMove={handleMove}
        onMouseUp={handleEnd}
        onMouseLeave={handleEnd}
        onTouchStart={handleStart}
        onTouchMove={handleMove}
        onTouchEnd={handleEnd}
      />
      <div className="sketch-toolbar">
        <div className="sketch-colors">
          {PALETTE.map((color, i) => (
            <button
              key={color}
              className={`sketch-color-swatch${colorIndex === i ? ' selected' : ''}`}
              style={{ background: color }}
              onClick={() => setColorIndex(i)}
              aria-label={`Color ${i + 1}`}
            />
          ))}
        </div>
        <div className="sketch-brush-sizes">
          {BRUSH_LABELS.map((label, i) => (
            <button
              key={label}
              className={`sketch-brush-btn${brushIndex === i ? ' selected' : ''}`}
              onClick={() => setBrushIndex(i)}
            >
              {label}
            </button>
          ))}
        </div>
      </div>
      <button className="btn" onClick={handleClear}>Clear</button>
    </div>
  );
}

export default function SketchDrawScreen({ game, playerId, secretAnswer, onDrawPoint, onDrawClear }) {
  if (!game) return null;
  const isMe = game.chosenId === playerId;

  if (!isMe) {
    return (
      <div className="screen">
        <p className="room-badge">Sketch & Guess — Round {game.index + 1} / {game.total}</p>
        <div className="cross">✝</div>
        <p className="question-text">{game.chosenName} is drawing!</p>
        <p className="subtitle">Watch the big screen — you'll guess what it is in a moment.</p>
      </div>
    );
  }

  return (
    <ArtistCanvas
      duration={game.duration}
      roundIndex={game.index}
      secretAnswer={secretAnswer}
      onDrawPoint={onDrawPoint}
      onDrawClear={onDrawClear}
    />
  );
}
