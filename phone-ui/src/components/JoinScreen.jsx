import React, { useState } from 'react';

export default function JoinScreen({ onJoin, error }) {
  const [name, setName] = useState('');
  const [code, setCode] = useState('');

  const canSubmit = name.trim().length > 0 && code.trim().length === 4;

  const submit = (e) => {
    e.preventDefault();
    if (!canSubmit) return;
    onJoin(name.trim(), code.trim());
  };

  return (
    <div className="screen">
      <div className="cross">✝</div>
      <h1 className="brand">JesBox</h1>
      <p className="subtitle">Enter the room code shown on the big screen</p>

      <form className="field" onSubmit={submit} style={{ alignItems: 'center' }}>
        <label htmlFor="code">Room Code</label>
        <input
          id="code"
          type="text"
          inputMode="text"
          autoCapitalize="characters"
          maxLength={4}
          placeholder="ABCD"
          value={code}
          onChange={(e) => setCode(e.target.value.toUpperCase().replace(/[^A-Z0-9]/g, ''))}
        />

        <label htmlFor="name">Your Name</label>
        <input
          id="name"
          type="text"
          maxLength={20}
          placeholder="Nickname"
          value={name}
          onChange={(e) => setName(e.target.value)}
        />

        <div className="error-text">{error}</div>

        <button type="submit" className="btn" disabled={!canSubmit}>
          Join Game
        </button>
      </form>
    </div>
  );
}
