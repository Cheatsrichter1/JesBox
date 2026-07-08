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
        public string Answer;
        public string[] Choices;
        public int CorrectIndex;

        public DrawPrompt(string answer, string[] choices, int correctIndex)
        {
            Answer = answer;
            Choices = choices;
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
            new DrawPrompt("Noah's Ark",
                new[] { "Jonah and the Whale", "Noah's Ark", "The Parting of the Red Sea", "The Tower of Babel" }, 1),
            new DrawPrompt("The Burning Bush",
                new[] { "The Ten Commandments", "The Nativity", "The Burning Bush", "Daniel in the Lions' Den" }, 2),
            new DrawPrompt("David and Goliath",
                new[] { "Daniel in the Lions' Den", "David and Goliath", "The Good Samaritan", "The Tower of Babel" }, 1),
            new DrawPrompt("Jonah and the Whale",
                new[] { "Noah's Ark", "The Parting of the Red Sea", "Jonah and the Whale", "Walking on Water" }, 2),
            new DrawPrompt("The Nativity",
                new[] { "The Burning Bush", "The Nativity", "The Good Samaritan", "Adam and Eve" }, 1),
            new DrawPrompt("Walking on Water",
                new[] { "Jonah and the Whale", "Walking on Water", "The Parting of the Red Sea", "Loaves and Fishes" }, 1),
            new DrawPrompt("The Ten Commandments",
                new[] { "The Burning Bush", "The Tower of Babel", "The Ten Commandments", "The Parting of the Red Sea" }, 2),
            new DrawPrompt("Loaves and Fishes",
                new[] { "The Nativity", "Loaves and Fishes", "Walking on Water", "The Good Samaritan" }, 1),
            new DrawPrompt("Adam and Eve",
                new[] { "The Prodigal Son", "The Tower of Babel", "Adam and Eve", "The Nativity" }, 2),
            new DrawPrompt("The Prodigal Son",
                new[] { "The Good Samaritan", "The Prodigal Son", "Adam and Eve", "David and Goliath" }, 1),
            new DrawPrompt("Daniel in the Lions' Den",
                new[] { "David and Goliath", "The Burning Bush", "Jonah and the Whale", "Daniel in the Lions' Den" }, 3),
            new DrawPrompt("The Good Samaritan",
                new[] { "The Prodigal Son", "Loaves and Fishes", "The Good Samaritan", "Adam and Eve" }, 2),
            new DrawPrompt("The Parting of the Red Sea",
                new[] { "Noah's Ark", "Jonah and the Whale", "Walking on Water", "The Parting of the Red Sea" }, 3),
            new DrawPrompt("The Tower of Babel",
                new[] { "Noah's Ark", "Adam and Eve", "The Tower of Babel", "The Ten Commandments" }, 2),
        };
    }
}
