import React from 'react';
import { useLanguage } from '../i18n.jsx';

export default function LobbyScreen({ roomCode, game }) {
  const { t } = useLanguage();
  const players = game?.players || [];

  return (
    <div className="screen">
      <div className="cross">✝</div>
      <h1 className="brand">{t('lobby.title')}</h1>
      <p className="room-badge">{t('lobby.room', { code: roomCode })}</p>
      <div className="spinner" />
      <p className="subtitle">{t('lobby.waitingHost')}</p>

      {players.length > 0 && (
        <div className="leaderboard">
          {players.map((p) => (
            <div className="leaderboard-row" key={p.id}>
              <span className="name-tag">{p.name}</span>
              <span>{t('lobby.ready')}</span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
