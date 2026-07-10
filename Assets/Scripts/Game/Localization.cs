using System.Collections.Generic;

namespace JesBox.Game
{
    public enum Language
    {
        English,
        German
    }

    /// <summary>
    /// TV-side (Unity) UI chrome strings. Content banks (trivia, microgames,
    /// vote prompts, solo games, draw prompts) carry their own English/German
    /// pairs directly and pick based on <see cref="Current"/> — this class is
    /// only for the fixed labels GameManager builds the lobby/panels out of.
    /// </summary>
    public static class L
    {
        public static Language Current = Language.English;

        private static readonly Dictionary<string, string[]> Strings = new Dictionary<string, string[]>
        {
            // key -> [English, German]
            ["lobby.title"] = new[] { "JESBOX", "JESBOX" },
            ["lobby.roomCode"] = new[] { "ROOM CODE: {0}", "RAUMCODE: {0}" },
            ["lobby.joinAt"] = new[] { "Join at {0}", "Beitreten unter {0}" },
            ["lobby.language"] = new[] { "LANGUAGE", "SPRACHE" },
            ["lobby.gameMode"] = new[] { "GAME MODE", "SPIELMODUS" },
            ["lobby.waiting"] = new[] { "Waiting for players...", "Warten auf Spieler..." },
            ["lobby.startGame"] = new[] { "Start Game", "Spiel starten" },
            ["lobby.playerReconnecting"] = new[] { "{0} (reconnecting...)", "{0} (verbindet erneut...)" },

            ["host.players"] = new[] { "Players", "Spieler" },
            ["host.playersTitle"] = new[] { "PLAYERS", "SPIELER" },
            ["host.close"] = new[] { "Close", "Schließen" },
            ["host.kick"] = new[] { "Kick", "Entfernen" },
            ["host.pause"] = new[] { "Pause", "Pause" },
            ["host.resume"] = new[] { "Resume", "Fortsetzen" },
            ["host.skip"] = new[] { "Skip", "Überspringen" },
            ["host.endGame"] = new[] { "End Game", "Spiel beenden" },

            ["mode.trivia"] = new[] { "Trivia Quiz", "Bibel-Quiz" },
            ["mode.microgames"] = new[] { "Microgames", "Minispiele" },
            ["mode.promptVote"] = new[] { "Prompt & Vote", "Abstimmung" },
            ["mode.chosenOne"] = new[] { "Chosen One", "Auserwählter" },
            ["mode.sketch"] = new[] { "Sketch & Guess", "Zeichnen & Raten" },
            ["mode.charades"] = new[] { "Bible Charades", "Bibel-Scharade" },

            ["difficulty.label"] = new[] { "DIFFICULTY", "SCHWIERIGKEIT" },
            ["difficulty.easy"] = new[] { "Easy", "Leicht" },
            ["difficulty.medium"] = new[] { "Medium", "Mittel" },
            ["difficulty.hard"] = new[] { "Hard", "Schwer" },

            ["stepper.questions"] = new[] { "Questions", "Fragen" },
            ["stepper.timeLimit"] = new[] { "Time Limit", "Zeitlimit" },
            ["stepper.microgameRounds"] = new[] { "Microgame Rounds", "Minispiel-Runden" },
            ["stepper.votePrompts"] = new[] { "Vote Prompts", "Abstimmungen" },
            ["stepper.turns"] = new[] { "Turns", "Runden" },
            ["stepper.sketchRounds"] = new[] { "Sketch Rounds", "Zeichenrunden" },
            ["stepper.charadeRounds"] = new[] { "Charade Rounds", "Scharade-Runden" },

            ["final.title"] = new[] { "FINAL SCORES", "ENDERGEBNIS" },
            ["final.backToMenu"] = new[] { "Back to Main Menu", "Zurück zum Hauptmenü" },

            ["question.header"] = new[] { "Question {0} / {1}", "Frage {0} / {1}" },
            ["question.correctAnswer"] = new[] { "Correct answer: {0}) {1}", "Richtige Antwort: {0}) {1}" },

            ["vote.header"] = new[] { "Vote! Prompt {0} / {1}", "Abstimmen! Vorgabe {0} / {1}" },
            ["vote.crowdFavorite"] = new[] { "Crowd favorite: {0}) {1}", "Publikumsliebling: {0}) {1}" },
            ["vote.noVotes"] = new[] { "No votes cast!", "Keine Stimmen abgegeben!" },

            ["microgame.header"] = new[] { "Microgame {0} / {1}", "Minispiel {0} / {1}" },
            ["microgame.results"] = new[] { "{0} — Results!", "{0} — Ergebnisse!" },

            ["solo.header"] = new[] { "Chosen One — Turn {0} / {1}", "Auserwählter — Runde {0} / {1}" },
            ["solo.isUp"] = new[] { "{0} is up!", "{0} ist dran!" },
            ["solo.cheer"] = new[] { "Everyone else, cheer them on — watch the screen!", "Alle anderen, feuert an — schaut auf den Bildschirm!" },

            ["sketch.header"] = new[] { "Sketch & Guess — Round {0} / {1}", "Zeichnen & Raten — Runde {0} / {1}" },
            ["sketch.isDrawing"] = new[] { "{0} is drawing...", "{0} zeichnet..." },
            ["sketch.title"] = new[] { "Sketch That Verse", "Zeichne die Szene" },
            ["sketch.watchHint"] = new[] { "Watch the sketch appear — get ready to guess!", "Schau zu, wie die Zeichnung entsteht — mach dich bereit zu raten!" },
            ["sketch.whatDidDraw"] = new[] { "What did {0} draw?", "Was hat {0} gezeichnet?" },
            ["sketch.guessOnPhone"] = new[] { "Guess on your phone!", "Rate auf deinem Handy!" },
            ["sketch.result"] = new[]
            {
                "{0} drew \"{1}\" — {2}/{3} guessed it! (+{4})",
                "{0} zeichnete \"{1}\" — {2}/{3} haben es erraten! (+{4})"
            },

            ["charade.header"] = new[] { "Bible Charades — Round {0} / {1}", "Bibel-Scharade — Runde {0} / {1}" },
            ["charade.actInstructions"] = new[]
            {
                "🤫 ACT IT OUT — no talking or pointing at objects! Everyone else, shout your guesses!",
                "🤫 SCHAUSPIELERN — nicht sprechen oder auf Gegenstände zeigen! Alle anderen, ruft eure Vermutungen!"
            },
            ["charade.describeInstructions"] = new[]
            {
                "🗣️ DESCRIBE IT — can't say the secret words! Everyone else, shout your guesses!",
                "🗣️ BESCHREIBEN — die geheimen Wörter dürfen nicht fallen! Alle anderen, ruft eure Vermutungen!"
            },
            ["charade.whatActed"] = new[] { "What did {0} act out?", "Was hat {0} vorgespielt?" },
            ["charade.whatDescribed"] = new[] { "What did {0} describe?", "Was hat {0} beschrieben?" },
            ["charade.result"] = new[]
            {
                "{0} needed everyone to guess \"{1}\" — {2}/{3} got it! (+{4})",
                "{0} brauchte die Hilfe aller, um \"{1}\" zu erraten — {2}/{3} haben es geschafft! (+{4})"
            },

            ["lobby.scanToJoin"] = new[] { "Scan to join!", "Zum Beitreten scannen!" },
        };

        public static string T(string key)
        {
            return Strings.TryGetValue(key, out var pair) ? pair[(int)Current] : key;
        }

        public static string T(string key, params object[] args)
        {
            string template = T(key);
            return args != null && args.Length > 0 ? string.Format(template, args) : template;
        }
    }
}
