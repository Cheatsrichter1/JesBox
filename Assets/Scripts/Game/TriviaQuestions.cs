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
        public string QuestionEn, QuestionDe;
        public string[] ChoicesEn, ChoicesDe;
        public int CorrectIndex;

        public string Question => L.Current == Language.German ? QuestionDe : QuestionEn;
        public string[] Choices => L.Current == Language.German ? ChoicesDe : ChoicesEn;

        public TriviaQuestion(Difficulty difficulty, string questionEn, string questionDe, string[] choicesEn, string[] choicesDe, int correctIndex)
        {
            Difficulty = difficulty;
            QuestionEn = questionEn;
            QuestionDe = questionDe;
            ChoicesEn = choicesEn;
            ChoicesDe = choicesDe;
            CorrectIndex = correctIndex;
        }
    }

    public static class TriviaQuestions
    {
        public static readonly List<TriviaQuestion> All = new List<TriviaQuestion>
        {
            // Easy
            new TriviaQuestion(Difficulty.Easy, "Who built the ark?", "Wer baute die Arche?",
                new[] { "Moses", "Noah", "Abraham", "Elijah" }, new[] { "Mose", "Noah", "Abraham", "Elia" }, 1),
            new TriviaQuestion(Difficulty.Easy, "What is the first book of the Bible?", "Was ist das erste Buch der Bibel?",
                new[] { "Exodus", "Genesis", "Leviticus", "Numbers" }, new[] { "Exodus", "Genesis", "Levitikus", "Numeri" }, 1),
            new TriviaQuestion(Difficulty.Easy, "Who was thrown into the lions' den?", "Wer wurde in die Löwengrube geworfen?",
                new[] { "David", "Samuel", "Daniel", "Solomon" }, new[] { "David", "Samuel", "Daniel", "Salomo" }, 2),
            new TriviaQuestion(Difficulty.Easy, "What did Jesus turn water into at the wedding at Cana?", "In was verwandelte Jesus Wasser bei der Hochzeit zu Kana?",
                new[] { "Bread", "Oil", "Wine", "Milk" }, new[] { "Brot", "Öl", "Wein", "Milch" }, 2),
            new TriviaQuestion(Difficulty.Easy, "Who led the Israelites out of Egypt?", "Wer führte die Israeliten aus Ägypten?",
                new[] { "Joshua", "Aaron", "Samuel", "Moses" }, new[] { "Josua", "Aaron", "Samuel", "Mose" }, 3),
            new TriviaQuestion(Difficulty.Easy, "How many disciples did Jesus choose?", "Wie viele Jünger wählte Jesus?",
                new[] { "10", "11", "12", "13" }, new[] { "10", "11", "12", "13" }, 2),
            new TriviaQuestion(Difficulty.Easy, "Who was swallowed by a great fish?", "Wer wurde von einem großen Fisch verschlungen?",
                new[] { "Job", "Jonah", "Daniel", "Elijah" }, new[] { "Hiob", "Jona", "Daniel", "Elia" }, 1),
            new TriviaQuestion(Difficulty.Easy, "What did David use to defeat Goliath?", "Womit besiegte David Goliath?",
                new[] { "A sword", "A spear", "A sling", "A bow" }, new[] { "Ein Schwert", "Ein Speer", "Eine Schleuder", "Ein Bogen" }, 2),

            // Medium
            new TriviaQuestion(Difficulty.Medium, "On what day of the week did God rest?", "An welchem Wochentag ruhte Gott?",
                new[] { "Fifth", "Sixth", "Seventh", "Eighth" }, new[] { "Fünfter", "Sechster", "Siebter", "Achter" }, 2),
            new TriviaQuestion(Difficulty.Medium, "How many days did God take to create the world before resting?", "Wie viele Tage brauchte Gott, um die Welt zu erschaffen, bevor er ruhte?",
                new[] { "5", "6", "7", "8" }, new[] { "5", "6", "7", "8" }, 1),
            new TriviaQuestion(Difficulty.Medium, "Who was Abraham's wife?", "Wer war Abrahams Frau?",
                new[] { "Rebekah", "Rachel", "Sarah", "Leah" }, new[] { "Rebekka", "Rahel", "Sara", "Lea" }, 2),
            new TriviaQuestion(Difficulty.Medium, "What sea did Moses part?", "Welches Meer teilte Mose?",
                new[] { "Sea of Galilee", "Dead Sea", "Red Sea", "Mediterranean Sea" }, new[] { "Galiläisches Meer", "Totes Meer", "Rotes Meer", "Mittelmeer" }, 2),
            new TriviaQuestion(Difficulty.Medium, "Who was thrown into a pit by his brothers and later sold into slavery?", "Wer wurde von seinen Brüdern in eine Grube geworfen und später als Sklave verkauft?",
                new[] { "Joseph", "Benjamin", "Reuben", "Judah" }, new[] { "Josef", "Benjamin", "Ruben", "Juda" }, 0),
            new TriviaQuestion(Difficulty.Medium, "What did God send to feed the Israelites in the wilderness?", "Was sandte Gott, um die Israeliten in der Wüste zu ernähren?",
                new[] { "Quail only", "Manna", "Locusts", "Honey" }, new[] { "Nur Wachteln", "Manna", "Heuschrecken", "Honig" }, 1),
            new TriviaQuestion(Difficulty.Medium, "Who denied knowing Jesus three times?", "Wer leugnete dreimal, Jesus zu kennen?",
                new[] { "John", "Peter", "Thomas", "Judas" }, new[] { "Johannes", "Petrus", "Thomas", "Judas" }, 1),
            new TriviaQuestion(Difficulty.Medium, "What instrument did David play?", "Welches Instrument spielte David?",
                new[] { "Trumpet", "Harp", "Flute", "Drum" }, new[] { "Trompete", "Harfe", "Flöte", "Trommel" }, 1),

            // Hard
            new TriviaQuestion(Difficulty.Hard, "How many years did the Israelites wander in the desert?", "Wie viele Jahre wanderten die Israeliten durch die Wüste?",
                new[] { "20", "30", "40", "50" }, new[] { "20", "30", "40", "50" }, 2),
            new TriviaQuestion(Difficulty.Hard, "Who was the mother of John the Baptist?", "Wer war die Mutter von Johannes dem Täufer?",
                new[] { "Elizabeth", "Anna", "Martha", "Rachel" }, new[] { "Elisabeth", "Anna", "Martha", "Rahel" }, 0),
            new TriviaQuestion(Difficulty.Hard, "What was the name of Ruth's mother-in-law?", "Wie hieß Ruths Schwiegermutter?",
                new[] { "Naomi", "Leah", "Deborah", "Miriam" }, new[] { "Noomi", "Lea", "Debora", "Mirjam" }, 0),
            new TriviaQuestion(Difficulty.Hard, "How many books are in the New Testament?", "Wie viele Bücher hat das Neue Testament?",
                new[] { "27", "29", "24", "31" }, new[] { "27", "29", "24", "31" }, 0),
            new TriviaQuestion(Difficulty.Hard, "Who succeeded Moses as leader of Israel?", "Wer wurde nach Mose Anführer Israels?",
                new[] { "Caleb", "Joshua", "Gideon", "Samuel" }, new[] { "Kaleb", "Josua", "Gideon", "Samuel" }, 1),
            new TriviaQuestion(Difficulty.Hard, "On the road to which city did Paul have his conversion experience?", "Auf dem Weg in welche Stadt erlebte Paulus seine Bekehrung?",
                new[] { "Jerusalem", "Damascus", "Antioch", "Ephesus" }, new[] { "Jerusalem", "Damaskus", "Antiochia", "Ephesus" }, 1),
            new TriviaQuestion(Difficulty.Hard, "Who was the first king of Israel?", "Wer war der erste König Israels?",
                new[] { "David", "Solomon", "Saul", "Samuel" }, new[] { "David", "Salomo", "Saul", "Samuel" }, 2),
            new TriviaQuestion(Difficulty.Hard, "What was Paul's name before his conversion?", "Wie hieß Paulus vor seiner Bekehrung?",
                new[] { "Silas", "Barnabas", "Saul", "Stephen" }, new[] { "Silas", "Barnabas", "Saulus", "Stephanus" }, 2),
        };

        public static List<TriviaQuestion> ForDifficulty(Difficulty difficulty)
        {
            return All.Where(q => q.Difficulty == difficulty).ToList();
        }
    }
}
