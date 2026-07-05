using System.Collections.Generic;

namespace JesBox.Game
{
    public class TriviaQuestion
    {
        public string Question;
        public string[] Choices;
        public int CorrectIndex;

        public TriviaQuestion(string question, string[] choices, int correctIndex)
        {
            Question = question;
            Choices = choices;
            CorrectIndex = correctIndex;
        }
    }

    public static class TriviaQuestions
    {
        public static readonly List<TriviaQuestion> All = new List<TriviaQuestion>
        {
            new TriviaQuestion("Who built the ark?", new[] { "Moses", "Noah", "Abraham", "Elijah" }, 1),
            new TriviaQuestion("How many days did God take to create the world before resting?", new[] { "5", "6", "7", "8" }, 1),
            new TriviaQuestion("What did Jesus turn water into at the wedding at Cana?", new[] { "Bread", "Oil", "Wine", "Milk" }, 2),
            new TriviaQuestion("Who led the Israelites out of Egypt?", new[] { "Joshua", "Aaron", "Samuel", "Moses" }, 3),
            new TriviaQuestion("How many disciples did Jesus choose?", new[] { "10", "11", "12", "13" }, 2),
            new TriviaQuestion("Who was swallowed by a great fish?", new[] { "Job", "Jonah", "Daniel", "Elijah" }, 1),
            new TriviaQuestion("What is the first book of the Bible?", new[] { "Exodus", "Genesis", "Leviticus", "Numbers" }, 1),
            new TriviaQuestion("Who was thrown into the lions' den?", new[] { "David", "Samuel", "Daniel", "Solomon" }, 2),
            new TriviaQuestion("What did David use to defeat Goliath?", new[] { "A sword", "A spear", "A sling", "A bow" }, 2),
            new TriviaQuestion("On what day of the week did God rest?", new[] { "Fifth", "Sixth", "Seventh", "Eighth" }, 2),
        };
    }
}
