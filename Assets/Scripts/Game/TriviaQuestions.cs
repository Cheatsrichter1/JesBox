using System.Collections.Generic;
using System.Linq;

namespace JesBox.Game
{
    public enum Difficulty
    {
        Easy,
        Medium,
        Hard
    }

    public class TriviaQuestion
    {
        public Difficulty Difficulty;
        public string Question;
        public string[] Choices;
        public int CorrectIndex;

        public TriviaQuestion(Difficulty difficulty, string question, string[] choices, int correctIndex)
        {
            Difficulty = difficulty;
            Question = question;
            Choices = choices;
            CorrectIndex = correctIndex;
        }
    }

    public static class TriviaQuestions
    {
        public static readonly List<TriviaQuestion> All = new List<TriviaQuestion>
        {
            // Easy
            new TriviaQuestion(Difficulty.Easy, "Who built the ark?", new[] { "Moses", "Noah", "Abraham", "Elijah" }, 1),
            new TriviaQuestion(Difficulty.Easy, "What is the first book of the Bible?", new[] { "Exodus", "Genesis", "Leviticus", "Numbers" }, 1),
            new TriviaQuestion(Difficulty.Easy, "Who was thrown into the lions' den?", new[] { "David", "Samuel", "Daniel", "Solomon" }, 2),
            new TriviaQuestion(Difficulty.Easy, "What did Jesus turn water into at the wedding at Cana?", new[] { "Bread", "Oil", "Wine", "Milk" }, 2),
            new TriviaQuestion(Difficulty.Easy, "Who led the Israelites out of Egypt?", new[] { "Joshua", "Aaron", "Samuel", "Moses" }, 3),
            new TriviaQuestion(Difficulty.Easy, "How many disciples did Jesus choose?", new[] { "10", "11", "12", "13" }, 2),
            new TriviaQuestion(Difficulty.Easy, "Who was swallowed by a great fish?", new[] { "Job", "Jonah", "Daniel", "Elijah" }, 1),
            new TriviaQuestion(Difficulty.Easy, "What did David use to defeat Goliath?", new[] { "A sword", "A spear", "A sling", "A bow" }, 2),

            // Medium
            new TriviaQuestion(Difficulty.Medium, "On what day of the week did God rest?", new[] { "Fifth", "Sixth", "Seventh", "Eighth" }, 2),
            new TriviaQuestion(Difficulty.Medium, "How many days did God take to create the world before resting?", new[] { "5", "6", "7", "8" }, 1),
            new TriviaQuestion(Difficulty.Medium, "Who was Abraham's wife?", new[] { "Rebekah", "Rachel", "Sarah", "Leah" }, 2),
            new TriviaQuestion(Difficulty.Medium, "What sea did Moses part?", new[] { "Sea of Galilee", "Dead Sea", "Red Sea", "Mediterranean Sea" }, 2),
            new TriviaQuestion(Difficulty.Medium, "Who was thrown into a pit by his brothers and later sold into slavery?", new[] { "Joseph", "Benjamin", "Reuben", "Judah" }, 0),
            new TriviaQuestion(Difficulty.Medium, "What did God send to feed the Israelites in the wilderness?", new[] { "Quail only", "Manna", "Locusts", "Honey" }, 1),
            new TriviaQuestion(Difficulty.Medium, "Who denied knowing Jesus three times?", new[] { "John", "Peter", "Thomas", "Judas" }, 1),
            new TriviaQuestion(Difficulty.Medium, "What instrument did David play?", new[] { "Trumpet", "Harp", "Flute", "Drum" }, 1),

            // Hard
            new TriviaQuestion(Difficulty.Hard, "How many years did the Israelites wander in the desert?", new[] { "20", "30", "40", "50" }, 2),
            new TriviaQuestion(Difficulty.Hard, "Who was the mother of John the Baptist?", new[] { "Elizabeth", "Anna", "Martha", "Rachel" }, 0),
            new TriviaQuestion(Difficulty.Hard, "What was the name of Ruth's mother-in-law?", new[] { "Naomi", "Leah", "Deborah", "Miriam" }, 0),
            new TriviaQuestion(Difficulty.Hard, "How many books are in the New Testament?", new[] { "27", "29", "24", "31" }, 0),
            new TriviaQuestion(Difficulty.Hard, "Who succeeded Moses as leader of Israel?", new[] { "Caleb", "Joshua", "Gideon", "Samuel" }, 1),
            new TriviaQuestion(Difficulty.Hard, "On the road to which city did Paul have his conversion experience?", new[] { "Jerusalem", "Damascus", "Antioch", "Ephesus" }, 1),
            new TriviaQuestion(Difficulty.Hard, "Who was the first king of Israel?", new[] { "David", "Solomon", "Saul", "Samuel" }, 2),
            new TriviaQuestion(Difficulty.Hard, "What was Paul's name before his conversion?", new[] { "Silas", "Barnabas", "Saul", "Stephen" }, 2),
        };

        public static List<TriviaQuestion> ForDifficulty(Difficulty difficulty)
        {
            return All.Where(q => q.Difficulty == difficulty).ToList();
        }
    }
}
