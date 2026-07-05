import React, { useCallback, useRef, useState } from 'react';
import JoinScreen from './components/JoinScreen.jsx';
import LobbyScreen from './components/LobbyScreen.jsx';
import QuestionScreen from './components/QuestionScreen.jsx';
import RevealScreen from './components/RevealScreen.jsx';
import FinalScreen from './components/FinalScreen.jsx';

function getWsUrl() {
  const proto = window.location.protocol === 'https:' ? 'wss' : 'ws';
  return `${proto}://${window.location.host}/ws`;
}

export default function App() {
  // 'join' | 'connecting' | 'lobby' | 'question' | 'reveal' | 'final'
  const [screen, setScreen] = useState('join');
  const [error, setError] = useState('');
  const [playerId, setPlayerId] = useState(null);
  const [roomCode, setRoomCode] = useState('');
  const [game, setGame] = useState(null); // last { phase, ... } payload from host
  const [selectedChoice, setSelectedChoice] = useState(null);
  const wsRef = useRef(null);

  const handleMessage = useCallback((raw) => {
    let msg;
    try {
      msg = JSON.parse(raw);
    } catch {
      return;
    }

    switch (msg.type) {
      case 'joined':
        setPlayerId(msg.playerId);
        setRoomCode(msg.roomCode);
        setError('');
        setScreen('lobby');
        break;
      case 'join_error':
        setError(msg.message || 'Could not join that room.');
        setScreen('join');
        if (wsRef.current) wsRef.current.close();
        break;
      case 'game': {
        const data = msg.data || {};
        setGame(data);
        if (data.phase === 'question') setSelectedChoice(null);
        if (data.phase) setScreen(data.phase);
        break;
      }
      case 'host_left':
        setError('The host disconnected. Ask them to relaunch and rejoin.');
        setScreen('join');
        break;
      default:
        break;
    }
  }, []);

  const join = useCallback((name, code) => {
    setError('');
    setScreen('connecting');
    const ws = new WebSocket(getWsUrl());
    wsRef.current = ws;

    ws.onopen = () => {
      ws.send(JSON.stringify({ type: 'join', roomCode: code.toUpperCase(), name }));
    };
    ws.onmessage = (event) => handleMessage(event.data);
    ws.onerror = () => {
      setError('Connection error. Check the room code and try again.');
      setScreen('join');
    };
    ws.onclose = () => {
      setScreen((current) => (current === 'connecting' ? 'join' : current));
    };
  }, [handleMessage]);

  const answer = useCallback((choiceIndex) => {
    if (selectedChoice !== null) return;
    setSelectedChoice(choiceIndex);
    const ws = wsRef.current;
    if (ws && ws.readyState === WebSocket.OPEN) {
      ws.send(JSON.stringify({ type: 'game', data: { action: 'answer', choice: choiceIndex } }));
    }
  }, [selectedChoice]);

  switch (screen) {
    case 'connecting':
      return (
        <div className="screen">
          <div className="spinner" />
          <p className="subtitle">Joining room...</p>
        </div>
      );
    case 'lobby':
      return <LobbyScreen roomCode={roomCode} game={game} />;
    case 'question':
      return (
        <QuestionScreen
          game={game}
          selectedChoice={selectedChoice}
          onAnswer={answer}
        />
      );
    case 'reveal':
      return <RevealScreen game={game} playerId={playerId} selectedChoice={selectedChoice} />;
    case 'final':
      return <FinalScreen game={game} playerId={playerId} />;
    case 'join':
    default:
      return <JoinScreen onJoin={join} error={error} />;
  }
}
