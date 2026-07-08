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
        public static readonly List<DrawPrompt> All = new List<DrawPrompt>
        {
            new DrawPrompt("Noah's Ark", new[] { "Noah's Ark", "A pirate ship", "The Titanic", "A submarine" }, 0),
            new DrawPrompt("The Burning Bush", new[] { "A campfire", "The Burning Bush", "A birthday cake", "A dragon" }, 1),
            new DrawPrompt("David and Goliath", new[] { "Jack and the Beanstalk", "David and Goliath", "A boxing match", "King Kong" }, 1),
            new DrawPrompt("Jonah and the Whale", new[] { "Pinocchio", "Moby Dick", "Jonah and the Whale", "A submarine" }, 2),
            new DrawPrompt("The Nativity", new[] { "The Nativity", "A camping trip", "A birthday party", "A petting zoo" }, 0),
            new DrawPrompt("Walking on Water", new[] { "Ice skating", "Walking on Water", "Surfing", "A magic trick" }, 1),
            new DrawPrompt("The Ten Commandments", new[] { "A grocery list", "A report card", "The Ten Commandments", "A treasure map" }, 2),
            new DrawPrompt("Loaves and Fishes", new[] { "A picnic", "A grocery store", "Loaves and Fishes", "A fishing trip" }, 2),
            new DrawPrompt("Adam and Eve", new[] { "Adam and Eve", "A couple gardening", "Tarzan and Jane", "Romeo and Juliet" }, 0),
            new DrawPrompt("The Prodigal Son", new[] { "A family reunion", "The Prodigal Son", "A graduation", "A homecoming parade" }, 1),
            new DrawPrompt("Daniel in the Lions' Den", new[] { "A zoo trip", "A circus act", "Daniel in the Lions' Den", "A safari" }, 2),
            new DrawPrompt("The Good Samaritan", new[] { "A car accident", "The Good Samaritan", "A hospital visit", "A road trip" }, 1),
        };
    }
}
