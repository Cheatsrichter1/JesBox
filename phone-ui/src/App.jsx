import React, { useCallback, useRef, useState } from 'react';
import JoinScreen from './components/JoinScreen.jsx';
import LobbyScreen from './components/LobbyScreen.jsx';
import QuestionScreen from './components/QuestionScreen.jsx';
import RevealScreen from './components/RevealScreen.jsx';
import FinalScreen from './components/FinalScreen.jsx';
import MicrogameScreen from './components/MicrogameScreen.jsx';
import RoundRevealScreen from './components/RoundRevealScreen.jsx';
import VotePromptScreen from './components/VotePromptScreen.jsx';
import VoteRevealScreen from './components/VoteRevealScreen.jsx';
import SoloTurnScreen from './components/SoloTurnScreen.jsx';
import SketchDrawScreen from './components/SketchDrawScreen.jsx';
import SketchGuessScreen from './components/SketchGuessScreen.jsx';
import CharadeTurnScreen from './components/CharadeTurnScreen.jsx';
import CharadeGuessScreen from './components/CharadeGuessScreen.jsx';
import LanguageToggle from './components/LanguageToggle.jsx';
import { LanguageProvider, useLanguage } from './i18n.jsx';

function getWsUrl() {
  const proto = window.location.protocol === 'https:' ? 'wss' : 'ws';
  return `${proto}://${window.location.host}/ws`;
}

function AppInner() {
  const { t } = useLanguage();

  // 'join' | 'connecting' | 'lobby' | 'question' | 'reveal' | 'microgame' |
  // 'microgame_reveal' | 'vote_prompt' | 'vote_reveal' | 'solo_turn' |
  // 'solo_reveal' | 'sketch_draw' | 'sketch_guess' | 'charade_turn' |
  // 'charade_guess' | 'final'
  const [screen, setScreen] = useState('join');
  const [error, setError] = useState('');
  const [playerId, setPlayerId] = useState(null);
  const [roomCode, setRoomCode] = useState('');
  const [game, setGame] = useState(null); // last { phase, ... } payload from host
  const [selectedChoice, setSelectedChoice] = useState(null);
  const [secretAnswer, setSecretAnswer] = useState(null); // Sketch That Verse: only set on the artist's phone
  const [charadeSecret, setCharadeSecret] = useState(null); // Bible Charades: only set on the performer's phone
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
        setError(msg.message || t('error.joinFailed'));
        setScreen('join');
        if (wsRef.current) wsRef.current.close();
        break;
      case 'game': {
        const data = msg.data || {};
        if (data.phase === 'sketch_answer') {
          // Targeted-only message (game_to) — only the artist's phone ever
          // receives this. Store it without touching `game`/`screen`.
          setSecretAnswer(data.answer);
          break;
        }
        if (data.phase === 'charade_secret') {
          // Targeted-only message (game_to) — only the performer's phone
          // ever receives this. Store it without touching `game`/`screen`.
          setCharadeSecret(data);
          break;
        }
        setGame(data);
        if (data.phase === 'question' || data.phase === 'vote_prompt' || data.phase === 'sketch_guess' || data.phase === 'charade_guess') setSelectedChoice(null);
        if (data.phase === 'sketch_draw') setSecretAnswer(null);
        if (data.phase === 'charade_turn') setCharadeSecret(null);
        if (data.phase) setScreen(data.phase);
        break;
      }
      case 'host_left':
        setError(t('error.hostLeft'));
        setScreen('join');
        break;
      default:
        break;
    }
  }, [t]);

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
      setError(t('error.connection'));
      setScreen('join');
    };
    ws.onclose = () => {
      setScreen((current) => (current === 'connecting' ? 'join' : current));
    };
  }, [handleMessage, t]);

  const sendAction = useCallback((payload) => {
    const ws = wsRef.current;
    if (ws && ws.readyState === WebSocket.OPEN) {
      ws.send(JSON.stringify({ type: 'game', data: payload }));
    }
  }, []);

  const answer = useCallback((choiceIndex) => {
    if (selectedChoice !== null) return;
    setSelectedChoice(choiceIndex);
    sendAction({ action: 'answer', choice: choiceIndex });
  }, [selectedChoice, sendAction]);

  const vote = useCallback((choiceIndex) => {
    if (selectedChoice !== null) return;
    setSelectedChoice(choiceIndex);
    sendAction({ action: 'vote', choice: choiceIndex });
  }, [selectedChoice, sendAction]);

  const tap = useCallback(() => {
    sendAction({ action: 'tap' });
  }, [sendAction]);

  const submitScore = useCallback((value) => {
    sendAction({ action: 'submit_score', value });
  }, [sendAction]);

  const move = useCallback((direction) => {
    sendAction({ action: 'move', choice: direction });
  }, [sendAction]);

  const fire = useCallback(() => {
    sendAction({ action: 'fire' });
  }, [sendAction]);

  const shake = useCallback(() => {
    sendAction({ action: 'shake' });
  }, [sendAction]);

  const drawPoint = useCallback((x, y, newStroke, colorIndex, brushSize) => {
    sendAction({ action: 'draw_point', x, y, newStroke, colorIndex, brushSize });
  }, [sendAction]);

  const drawClear = useCallback(() => {
    sendAction({ action: 'draw_clear' });
  }, [sendAction]);

  let content;
  switch (screen) {
    case 'connecting':
      content = (
        <div className="screen">
          <div className="spinner" />
          <p className="subtitle">{t('connecting.text')}</p>
        </div>
      );
      break;
    case 'lobby':
      content = <LobbyScreen roomCode={roomCode} game={game} />;
      break;
    case 'question':
      content = <QuestionScreen game={game} selectedChoice={selectedChoice} onAnswer={answer} />;
      break;
    case 'reveal':
      content = <RevealScreen game={game} playerId={playerId} selectedChoice={selectedChoice} />;
      break;
    case 'microgame':
      content = <MicrogameScreen game={game} onTap={tap} onSubmitScore={submitScore} />;
      break;
    case 'microgame_reveal':
      content = <RoundRevealScreen game={game} playerId={playerId} />;
      break;
    case 'vote_prompt':
      content = <VotePromptScreen game={game} selectedChoice={selectedChoice} onVote={vote} />;
      break;
    case 'vote_reveal':
      content = <VoteRevealScreen game={game} playerId={playerId} />;
      break;
    case 'solo_turn':
      content = <SoloTurnScreen game={game} playerId={playerId} onMove={move} onFire={fire} onShake={shake} />;
      break;
    case 'solo_reveal':
      content = <RoundRevealScreen game={game} playerId={playerId} />;
      break;
    case 'sketch_draw':
      content = (
        <SketchDrawScreen
          game={game}
          playerId={playerId}
          secretAnswer={secretAnswer}
          onDrawPoint={drawPoint}
          onDrawClear={drawClear}
        />
      );
      break;
    case 'sketch_guess':
      content = <SketchGuessScreen game={game} playerId={playerId} selectedChoice={selectedChoice} onAnswer={answer} />;
      break;
    case 'charade_turn':
      content = <CharadeTurnScreen game={game} playerId={playerId} secret={charadeSecret} />;
      break;
    case 'charade_guess':
      content = <CharadeGuessScreen game={game} playerId={playerId} selectedChoice={selectedChoice} onAnswer={answer} />;
      break;
    case 'final':
      content = <FinalScreen game={game} playerId={playerId} />;
      break;
    case 'join':
    default:
      content = <JoinScreen onJoin={join} error={error} />;
      break;
  }

  return (
    <>
      <LanguageToggle />
      {content}
    </>
  );
}

export default function App() {
  return (
    <LanguageProvider>
      <AppInner />
    </LanguageProvider>
  );
}
