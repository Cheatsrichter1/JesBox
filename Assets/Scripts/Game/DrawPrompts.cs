using System.Collections.Generic;

namespace JesBox.Game
{
    /// <summary>
    /// A thing for the Chosen One to draw. <see cref="Choices"/> is the
    /// multiple-choice list shown to everyone else afterward; <see cref="CorrectIndex"/>
    /// points at the entry matching <see cref="Answer"/>.
    /// </summary>
    public class DrawPrompt
    {
        public string AnswerEn, AnswerDe;
        public string[] ChoicesEn, ChoicesDe;
        public int CorrectIndex;

        public string Answer => L.Current == Language.German ? AnswerDe : AnswerEn;
        public string[] Choices => L.Current == Language.German ? ChoicesDe : ChoicesEn;

        public DrawPrompt(string answerEn, string answerDe, string[] choicesEn, string[] choicesDe, int correctIndex)
        {
            AnswerEn = answerEn;
            AnswerDe = answerDe;
            ChoicesEn = choicesEn;
            ChoicesDe = choicesDe;
            CorrectIndex = correctIndex;
        }
    }

    public static class DrawPrompts
    {
        // Every choice here is a real Bible scene — no throwaway joke options
        // to eliminate on sight. Decoys are picked to share visual elements
        // with the correct answer (boats/water, Moses, miracles, parables),
        // so guessing takes actually reading the drawing, not just process
        // of elimination.
        public static readonly List<DrawPrompt> All = new List<DrawPrompt>
        {
            new DrawPrompt("Noah's Ark", "Noahs Arche",
                new[] { "Jonah and the Whale", "Noah's Ark", "The Parting of the Red Sea", "The Tower of Babel" },
                new[] { "Jona und der Wal", "Noahs Arche", "Die Teilung des Roten Meeres", "Der Turmbau zu Babel" }, 1),
            new DrawPrompt("The Burning Bush", "Der brennende Dornbusch",
                new[] { "The Ten Commandments", "The Nativity", "The Burning Bush", "Daniel in the Lions' Den" },
                new[] { "Die Zehn Gebote", "Die Geburt Jesu", "Der brennende Dornbusch", "Daniel in der Löwengrube" }, 2),
            new DrawPrompt("David and Goliath", "David und Goliath",
                new[] { "Daniel in the Lions' Den", "David and Goliath", "The Good Samaritan", "The Tower of Babel" },
                new[] { "Daniel in der Löwengrube", "David und Goliath", "Der barmherzige Samariter", "Der Turmbau zu Babel" }, 1),
            new DrawPrompt("Jonah and the Whale", "Jona und der Wal",
                new[] { "Noah's Ark", "The Parting of the Red Sea", "Jonah and the Whale", "Walking on Water" },
                new[] { "Noahs Arche", "Die Teilung des Roten Meeres", "Jona und der Wal", "Der Wandel auf dem Wasser" }, 2),
            new DrawPrompt("The Nativity", "Die Geburt Jesu",
                new[] { "The Burning Bush", "The Nativity", "The Good Samaritan", "Adam and Eve" },
                new[] { "Der brennende Dornbusch", "Die Geburt Jesu", "Der barmherzige Samariter", "Adam und Eva" }, 1),
            new DrawPrompt("Walking on Water", "Der Wandel auf dem Wasser",
                new[] { "Jonah and the Whale", "Walking on Water", "The Parting of the Red Sea", "Loaves and Fishes" },
                new[] { "Jona und der Wal", "Der Wandel auf dem Wasser", "Die Teilung des Roten Meeres", "Brote und Fische" }, 1),
            new DrawPrompt("The Ten Commandments", "Die Zehn Gebote",
                new[] { "The Burning Bush", "The Tower of Babel", "The Ten Commandments", "The Parting of the Red Sea" },
                new[] { "Der brennende Dornbusch", "Der Turmbau zu Babel", "Die Zehn Gebote", "Die Teilung des Roten Meeres" }, 2),
            new DrawPrompt("Loaves and Fishes", "Brote und Fische",
                new[] { "The Nativity", "Loaves and Fishes", "Walking on Water", "The Good Samaritan" },
                new[] { "Die Geburt Jesu", "Brote und Fische", "Der Wandel auf dem Wasser", "Der barmherzige Samariter" }, 1),
            new DrawPrompt("Adam and Eve", "Adam und Eva",
                new[] { "The Prodigal Son", "The Tower of Babel", "Adam and Eve", "The Nativity" },
                new[] { "Der verlorene Sohn", "Der Turmbau zu Babel", "Adam und Eva", "Die Geburt Jesu" }, 2),
            new DrawPrompt("The Prodigal Son", "Der verlorene Sohn",
                new[] { "The Good Samaritan", "The Prodigal Son", "Adam and Eve", "David and Goliath" },
                new[] { "Der barmherzige Samariter", "Der verlorene Sohn", "Adam und Eva", "David und Goliath" }, 1),
            new DrawPrompt("Daniel in the Lions' Den", "Daniel in der Löwengrube",
                new[] { "David and Goliath", "The Burning Bush", "Jonah and the Whale", "Daniel in the Lions' Den" },
                new[] { "David und Goliath", "Der brennende Dornbusch", "Jona und der Wal", "Daniel in der Löwengrube" }, 3),
            new DrawPrompt("The Good Samaritan", "Der barmherzige Samariter",
                new[] { "The Prodigal Son", "Loaves and Fishes", "The Good Samaritan", "Adam and Eve" },
                new[] { "Der verlorene Sohn", "Brote und Fische", "Der barmherzige Samariter", "Adam und Eva" }, 2),
            new DrawPrompt("The Parting of the Red Sea", "Die Teilung des Roten Meeres",
                new[] { "Noah's Ark", "Jonah and the Whale", "Walking on Water", "The Parting of the Red Sea" },
                new[] { "Noahs Arche", "Jona und der Wal", "Der Wandel auf dem Wasser", "Die Teilung des Roten Meeres" }, 3),
            new DrawPrompt("The Tower of Babel", "Der Turmbau zu Babel",
                new[] { "Noah's Ark", "Adam and Eve", "The Tower of Babel", "The Ten Commandments" },
                new[] { "Noahs Arche", "Adam und Eva", "Der Turmbau zu Babel", "Die Zehn Gebote" }, 2),
        };
    }
}
