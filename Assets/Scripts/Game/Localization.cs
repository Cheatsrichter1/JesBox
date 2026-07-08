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

            ["mode.trivia"] = new[] { "Trivia Quiz", "Bibel-Quiz" },
            ["mode.microgames"] = new[] { "Microgames", "Minispiele" },
            ["mode.promptVote"] = new[] { "Prompt & Vote", "Abstimmung" },
            ["mode.chosenOne"] = new[] { "Chosen One", "Auserwählter" },
            ["mode.sketch"] = new[] { "Sketch & Guess", "Zeichnen & Raten" },

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
