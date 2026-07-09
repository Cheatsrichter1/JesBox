import React, { createContext, useContext, useMemo, useState } from 'react';

// Phone UI chrome only — game content (trivia questions, vote prompts,
// microgame/solo instructions, draw prompts/choices) is already in the
// right language by the time it arrives from Unity, since the host picks
// its own language independently in the lobby. See Assets/Scripts/Game/Localization.cs.
const translations = {
  en: {
    'join.subtitle': 'Enter the room code shown on the big screen',
    'join.roomCodeLabel': 'Room Code',
    'join.nameLabel': 'Your Name',
    'join.namePlaceholder': 'Nickname',
    'join.button': 'Join Game',

    'connecting.text': 'Joining room...',

    'error.joinFailed': 'Could not join that room.',
    'error.connection': 'Connection error. Check the room code and try again.',
    'error.hostLeft': 'The host disconnected. Ask them to relaunch and rejoin.',

    'lobby.title': "You're in!",
    'lobby.room': 'Room {code}',
    'lobby.waitingHost': 'Waiting for the host to start the game...',
    'lobby.ready': 'ready',

    'question.header': 'Question {n} of {total}',
    'question.locked': 'Answer locked in — waiting for others...',

    'reveal.timeUp': "Time's up!",
    'reveal.correct': 'Correct! ✝',
    'reveal.notQuite': 'Not quite',
    'reveal.pointsPrefix': '+{delta} points — ',
    'reveal.total': 'Total: {score}',

    'roundReveal.defaultTitle': 'Round Results!',
    'roundReveal.noPointsPrefix': 'No points this round — ',

    'vote.header': 'Vote! Prompt {n} of {total}',
    'vote.locked': 'Vote locked in — waiting for others...',
    'voteReveal.crowdFavorite': 'Crowd favorite: {letter}',
    'voteReveal.noVotes': 'No votes cast!',

    'microgame.header': 'Microgame {n} / {total}',
    'microgame.gathered': '{n} gathered',
    'microgame.tap': 'TAP!',
    'microgame.score': 'Score: {n}',
    'microgame.shakes': '{n} shakes',
    'microgame.tapOrShake': 'TAP (or shake)!',
    'microgame.multiply': 'MULTIPLY!',
    'microgame.swipes': '{n} swipes',

    'solo.header': 'Chosen One — Turn {n} / {total}',
    'solo.turnAnnounce': "{name}'s turn!",
    'solo.watchCheer': "Look up at the screen and cheer them on — you're up soon!",
    'solo.watchHint': '👀 Watch the big screen!',
    'solo.dirLeft': '◀ LEFT',
    'solo.dirRight': 'RIGHT ▶',
    'solo.fire': '🔥 FIRE!',
    'solo.multiply': '🍞 MULTIPLY!',
    'solo.go': 'GO!',
    'solo.shakeReady': '📳 SHAKE!',
    'solo.shakeNotReady': '🙏 PRAY (tap or shake)!',

    'sketch.roomBadge': 'Sketch That Verse',
    'sketch.drawLabel': 'Draw: {answer}',
    'sketch.clearButton': 'Clear',
    'sketch.roundHeader': 'Sketch & Guess — Round {n} / {total}',
    'sketch.isDrawingWatchers': '{name} is drawing!',
    'sketch.watchBigScreen': "Watch the big screen — you'll guess what it is in a moment.",
    'sketch.waitingGuesses': 'Waiting for everyone to guess your drawing...',
    'sketch.whatDidDraw': 'What did {name} draw?',
    'sketch.guessLocked': 'Guess locked in — waiting for others...',

    'charade.roundHeader': 'Bible Charades — Round {n} / {total}',
    'charade.isPerformingWatchers': '{name} is performing!',
    'charade.watchAndGuess': 'Watch closely and shout out your guesses!',
    'charade.actLabel': 'Act out: {prompt}',
    'charade.describeLabel': 'Describe: {prompt}',
    'charade.forbiddenWords': "Don't say: {words}",
    'charade.actInstructionsPhone': "🤫 No talking or pointing at objects!",
    'charade.describeInstructionsPhone': "🗣️ Don't say the forbidden words!",
    'charade.waitingGuesses': 'Waiting for everyone to guess...',
    'charade.whatWasPerformed': 'What was {name} performing?',
    'charade.guessLocked': 'Guess locked in — waiting for others...',

    'final.title': 'Final Scores',
    'final.ledFlock': 'You led the flock! 🎉',
    'final.thanks': 'Thanks for playing — ask the host to start a new game.',
  },
  de: {
    'join.subtitle': 'Gib den Raumcode ein, der auf dem großen Bildschirm angezeigt wird',
    'join.roomCodeLabel': 'Raumcode',
    'join.nameLabel': 'Dein Name',
    'join.namePlaceholder': 'Spitzname',
    'join.button': 'Spiel beitreten',

    'connecting.text': 'Trete Raum bei...',

    'error.joinFailed': 'Dieser Raum wurde nicht gefunden.',
    'error.connection': 'Verbindungsfehler. Überprüfe den Raumcode und versuche es erneut.',
    'error.hostLeft': 'Der Host hat die Verbindung getrennt. Bitte neu starten und erneut beitreten.',

    'lobby.title': 'Du bist dabei!',
    'lobby.room': 'Raum {code}',
    'lobby.waitingHost': 'Warten, bis der Host das Spiel startet...',
    'lobby.ready': 'bereit',

    'question.header': 'Frage {n} von {total}',
    'question.locked': 'Antwort gesperrt — warte auf die anderen...',

    'reveal.timeUp': 'Die Zeit ist um!',
    'reveal.correct': 'Richtig! ✝',
    'reveal.notQuite': 'Nicht ganz',
    'reveal.pointsPrefix': '+{delta} Punkte — ',
    'reveal.total': 'Gesamt: {score}',

    'roundReveal.defaultTitle': 'Rundenergebnis!',
    'roundReveal.noPointsPrefix': 'Keine Punkte in dieser Runde — ',

    'vote.header': 'Abstimmen! Vorgabe {n} von {total}',
    'vote.locked': 'Stimme gesperrt — warte auf die anderen...',
    'voteReveal.crowdFavorite': 'Publikumsliebling: {letter}',
    'voteReveal.noVotes': 'Keine Stimmen abgegeben!',

    'microgame.header': 'Minispiel {n} / {total}',
    'microgame.gathered': '{n} gesammelt',
    'microgame.tap': 'TIPPEN!',
    'microgame.score': 'Punkte: {n}',
    'microgame.shakes': '{n} mal geschüttelt',
    'microgame.tapOrShake': 'TIPPEN (oder schütteln)!',
    'microgame.multiply': 'VERMEHREN!',
    'microgame.swipes': '{n} Wischer',

    'solo.header': 'Auserwählter — Runde {n} / {total}',
    'solo.turnAnnounce': '{name} ist dran!',
    'solo.watchCheer': 'Schau auf den Bildschirm und feuere an — du bist bald dran!',
    'solo.watchHint': '👀 Schau auf den großen Bildschirm!',
    'solo.dirLeft': '◀ LINKS',
    'solo.dirRight': 'RECHTS ▶',
    'solo.fire': '🔥 FEUER!',
    'solo.multiply': '🍞 VERMEHREN!',
    'solo.go': 'LOS!',
    'solo.shakeReady': '📳 SCHÜTTELN!',
    'solo.shakeNotReady': '🙏 BETEN (tippen oder schütteln)!',

    'sketch.roomBadge': 'Zeichne die Szene',
    'sketch.drawLabel': 'Zeichne: {answer}',
    'sketch.clearButton': 'Löschen',
    'sketch.roundHeader': 'Zeichnen & Raten — Runde {n} / {total}',
    'sketch.isDrawingWatchers': '{name} zeichnet gerade!',
    'sketch.watchBigScreen': 'Schau auf den großen Bildschirm — gleich darfst du raten.',
    'sketch.waitingGuesses': 'Warten, bis alle deine Zeichnung erraten haben...',
    'sketch.whatDidDraw': 'Was hat {name} gezeichnet?',
    'sketch.guessLocked': 'Vermutung gesperrt — warte auf die anderen...',

    'charade.roundHeader': 'Bibel-Scharade — Runde {n} / {total}',
    'charade.isPerformingWatchers': '{name} spielt gerade vor!',
    'charade.watchAndGuess': 'Schau genau hin und ruf deine Vermutungen!',
    'charade.actLabel': 'Spiel vor: {prompt}',
    'charade.describeLabel': 'Beschreibe: {prompt}',
    'charade.forbiddenWords': 'Nicht sagen: {words}',
    'charade.actInstructionsPhone': '🤫 Nicht sprechen oder auf Gegenstände zeigen!',
    'charade.describeInstructionsPhone': '🗣️ Die geheimen Wörter dürfen nicht fallen!',
    'charade.waitingGuesses': 'Warten, bis alle geraten haben...',
    'charade.whatWasPerformed': 'Was hat {name} vorgespielt?',
    'charade.guessLocked': 'Vermutung gesperrt — warte auf die anderen...',

    'final.title': 'Endergebnis',
    'final.ledFlock': 'Du hast die Herde angeführt! 🎉',
    'final.thanks': 'Danke fürs Mitspielen — bittet den Host, ein neues Spiel zu starten.',
  },
};

const LanguageContext = createContext({ lang: 'en', setLang: () => {}, t: (key) => key });

function interpolate(template, vars) {
  if (!vars) return template;
  return template.replace(/\{(\w+)\}/g, (match, key) => (key in vars ? vars[key] : match));
}

export function LanguageProvider({ children }) {
  const [lang, setLangState] = useState(() => {
    try {
      return localStorage.getItem('jesbox_lang') || 'en';
    } catch {
      return 'en';
    }
  });

  const setLang = (next) => {
    setLangState(next);
    try {
      localStorage.setItem('jesbox_lang', next);
    } catch {
      // ignore (private browsing etc.)
    }
  };

  const t = useMemo(() => {
    return (key, vars) => {
      const table = translations[lang] || translations.en;
      const template = table[key] ?? translations.en[key] ?? key;
      return interpolate(template, vars);
    };
  }, [lang]);

  return (
    <LanguageContext.Provider value={{ lang, setLang, t }}>
      {children}
    </LanguageContext.Provider>
  );
}

export function useLanguage() {
  return useContext(LanguageContext);
}
