import React, { useCallback, useEffect, useRef, useState } from 'react';
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

// Session persistence so a dropped connection (network blip, backgrounded
// tab, accidental reload) can reclaim the same playerId — and therefore the
// same score, which the host keeps keyed by playerId — instead of joining
// fresh. Scoped to sessionStorage (not localStorage) so it naturally expires
// with the tab rather than resurrecting a session from a long-gone game.
const SESSION_KEY = 'jesbox_session';

function loadSession() {
  try {
    const raw = sessionStorage.getItem(SESSION_KEY);
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
}

function saveSession(session) {
  try {
    sessionStorage.setItem(SESSION_KEY, JSON.stringify(session));
  } catch {
    // ignore (private browsing etc.)
  }
}

function clearSession() {
  try {
    sessionStorage.removeItem(SESSION_KEY);
  } catch {
    // ignore
  }
}

const RECONNECT_MAX_ATTEMPTS = 5;
const RECONNECT_BASE_DELAY_MS = 1000;

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
  const sessionRef = useRef(null); // {roomCode, playerId, name} once joined
  const pendingNameRef = useRef('');
  const isRejoinRef = useRef(false);
  const reconnectAttemptRef = useRef(0);
  const reconnectTimerRef = useRef(null);

  const handleMessage = useCallback((raw) => {
    let msg;
    try {
      msg = JSON.parse(raw);
    } catch {
      return;
    }

    switch (msg.type) {
      case 'joined': {
        reconnectAttemptRef.current = 0;
        const session = { roomCode: msg.roomCode, playerId: msg.playerId, name: sessionRef.current?.name || pendingNameRef.current };
        sessionRef.current = session;
        saveSession(session);
        setPlayerId(msg.playerId);
        setRoomCode(msg.roomCode);
        setError('');
        // On a fresh join, jump straight to the lobby as before. On a
        // rejoin, stay on the spinner — the host resyncs us with a `game`
        // message a moment later carrying whatever phase is actually live,
        // and that's what should drive the screen switch.
        if (!isRejoinRef.current) setScreen('lobby');
        break;
      }
      case 'join_error':
        clearSession();
        sessionRef.current = null;
        isRejoinRef.current = false;
        reconnectAttemptRef.current = 0;
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
        clearSession();
        sessionRef.current = null;
        setError(t('error.hostLeft'));
        setScreen('join');
        break;
      default:
        break;
    }
  }, [t]);

  // openSocket/scheduleReconnect call each other, so both are plain function
  // declarations (hoisted, and cheap to recreate each render) rather than
  // useCallback — that sidesteps a circular-dependency headache for no real
  // cost, since neither is passed down as a memoized child prop.
  function openSocket(buildInitialMessage) {
    const ws = new WebSocket(getWsUrl());
    wsRef.current = ws;

    ws.onopen = () => ws.send(JSON.stringify(buildInitialMessage()));
    ws.onmessage = (event) => handleMessage(event.data);
    ws.onerror = () => {
      if (wsRef.current !== ws || ws._jesboxHandled) return;
      ws._jesboxHandled = true;
      scheduleReconnect();
    };
    ws.onclose = () => {
      if (wsRef.current !== ws || ws._jesboxHandled) return;
      ws._jesboxHandled = true;
      scheduleReconnect();
    };
  }

  function scheduleReconnect() {
    if (reconnectTimerRef.current) {
      clearTimeout(reconnectTimerRef.current);
      reconnectTimerRef.current = null;
    }

    if (!sessionRef.current) {
      setScreen('join');
      return;
    }

    if (reconnectAttemptRef.current >= RECONNECT_MAX_ATTEMPTS) {
      clearSession();
      sessionRef.current = null;
      isRejoinRef.current = false;
      setScreen('join');
      setError(t('error.connectionLost'));
      return;
    }

    const attempt = reconnectAttemptRef.current;
    reconnectAttemptRef.current += 1;
    isRejoinRef.current = true;
    setScreen('connecting');

    const delay = RECONNECT_BASE_DELAY_MS * Math.pow(2, attempt);
    reconnectTimerRef.current = setTimeout(() => {
      const session = sessionRef.current;
      if (!session) return;
      openSocket(() => ({ type: 'rejoin', roomCode: session.roomCode, playerId: session.playerId }));
    }, delay);
  }

  // If the tab reloaded (or the app relaunched) while a session was still
  // active, try to silently pick back up instead of dumping the player onto
  // the join screen.
  useEffect(() => {
    const stored = loadSession();
    if (!stored) return;
    sessionRef.current = stored;
    isRejoinRef.current = true;
    setScreen('connecting');
    openSocket(() => ({ type: 'rejoin', roomCode: stored.roomCode, playerId: stored.playerId }));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const join = useCallback((name, code) => {
    setError('');
    clearSession();
    sessionRef.current = null;
    isRejoinRef.current = false;
    reconnectAttemptRef.current = 0;
    pendingNameRef.current = name;
    setScreen('connecting');
    openSocket(() => ({ type: 'join', roomCode: code.toUpperCase(), name }));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

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
          <p className="subtitle">{t(isRejoinRef.current ? 'connecting.reconnecting' : 'connecting.text')}</p>
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
