using System.Collections.Generic;

namespace JesBox.Game
{
    /// <summary>
    /// A thing to act out or describe. <see cref="Forbidden"/> only matters
    /// for the "describe" round type (Taboo-style words the describer can't
    /// say) — Act rounds ignore it. <see cref="Choices"/>/<see cref="CorrectIndex"/>
    /// work exactly like <see cref="DrawPrompt"/>.
    /// </summary>
    public class CharadePrompt
    {
        public string PromptEn, PromptDe;
        public string[] ForbiddenEn, ForbiddenDe;
        public string[] ChoicesEn, ChoicesDe;
        public int CorrectIndex;

        public string Prompt => L.Current == Language.German ? PromptDe : PromptEn;
        public string[] Forbidden => L.Current == Language.German ? ForbiddenDe : ForbiddenEn;
        public string[] Choices => L.Current == Language.German ? ChoicesDe : ChoicesEn;

        public CharadePrompt(string promptEn, string promptDe, string[] forbiddenEn, string[] forbiddenDe,
            string[] choicesEn, string[] choicesDe, int correctIndex)
        {
            PromptEn = promptEn;
            PromptDe = promptDe;
            ForbiddenEn = forbiddenEn;
            ForbiddenDe = forbiddenDe;
            ChoicesEn = choicesEn;
            ChoicesDe = choicesDe;
            CorrectIndex = correctIndex;
        }
    }

    public static class CharadePrompts
    {
        public static readonly List<CharadePrompt> All = new List<CharadePrompt>
        {
            new CharadePrompt("Noah's Ark", "Noahs Arche",
                new[] { "Ark", "Flood", "Boat", "Animals", "Rain" }, new[] { "Arche", "Flut", "Boot", "Tiere", "Regen" },
                new[] { "Noah's Ark", "The Tower of Babel", "The Parting of the Red Sea", "Jonah and the Whale" },
                new[] { "Noahs Arche", "Der Turmbau zu Babel", "Die Teilung des Roten Meeres", "Jona und der Wal" }, 0),
            new CharadePrompt("David and Goliath", "David und Goliath",
                new[] { "Giant", "Sling", "Stone", "Philistine" }, new[] { "Riese", "Schleuder", "Stein", "Philister" },
                new[] { "Samson", "David and Goliath", "Daniel", "Joshua" },
                new[] { "Simson", "David und Goliath", "Daniel", "Josua" }, 1),
            new CharadePrompt("The Nativity", "Die Geburt Jesu",
                new[] { "Baby", "Jesus", "Manger", "Bethlehem", "Star" }, new[] { "Baby", "Jesus", "Krippe", "Bethlehem", "Stern" },
                new[] { "The Nativity", "Easter", "Passover", "Pentecost" },
                new[] { "Die Geburt Jesu", "Ostern", "Passah", "Pfingsten" }, 0),
            new CharadePrompt("Jonah and the Whale", "Jona und der Wal",
                new[] { "Whale", "Fish", "Storm", "Ship" }, new[] { "Wal", "Fisch", "Sturm", "Schiff" },
                new[] { "Noah's Ark", "Jonah and the Whale", "Moses", "Elijah" },
                new[] { "Noahs Arche", "Jona und der Wal", "Mose", "Elia" }, 1),
            new CharadePrompt("Moses and the Burning Bush", "Mose und der brennende Dornbusch",
                new[] { "Bush", "Fire", "Moses", "Voice" }, new[] { "Dornbusch", "Feuer", "Mose", "Stimme" },
                new[] { "Moses and the Burning Bush", "The Ten Commandments", "The Exodus", "Joseph's Dream" },
                new[] { "Mose und der brennende Dornbusch", "Die Zehn Gebote", "Der Auszug aus Ägypten", "Josefs Traum" }, 0),
            new CharadePrompt("The Ten Commandments", "Die Zehn Gebote",
                new[] { "Tablets", "Mountain", "Moses", "Stone", "Rules" }, new[] { "Tafeln", "Berg", "Mose", "Stein", "Regeln" },
                new[] { "The Ten Commandments", "The Ark of the Covenant", "The Golden Calf", "The Burning Bush" },
                new[] { "Die Zehn Gebote", "Die Bundeslade", "Das goldene Kalb", "Der brennende Dornbusch" }, 0),
            new CharadePrompt("Walking on Water", "Der Wandel auf dem Wasser",
                new[] { "Water", "Peter", "Storm", "Boat", "Sink" }, new[] { "Wasser", "Petrus", "Sturm", "Boot", "Sinken" },
                new[] { "Walking on Water", "Jonah and the Whale", "The Parting of the Red Sea", "Feeding the 5000" },
                new[] { "Der Wandel auf dem Wasser", "Jona und der Wal", "Die Teilung des Roten Meeres", "Die Speisung der 5000" }, 0),
            new CharadePrompt("Feeding the 5000", "Die Speisung der 5000",
                new[] { "Bread", "Fish", "Crowd", "Basket", "Multiply" }, new[] { "Brot", "Fisch", "Menge", "Korb", "Vermehren" },
                new[] { "Feeding the 5000", "The Last Supper", "The Wedding at Cana", "The Nativity" },
                new[] { "Die Speisung der 5000", "Das letzte Abendmahl", "Die Hochzeit zu Kana", "Die Geburt Jesu" }, 0),
            new CharadePrompt("Daniel in the Lions' Den", "Daniel in der Löwengrube",
                new[] { "Lions", "Den", "Daniel", "King", "Cage" }, new[] { "Löwen", "Grube", "Daniel", "König", "Käfig" },
                new[] { "David and Goliath", "Daniel in the Lions' Den", "Samson", "Noah's Ark" },
                new[] { "David und Goliath", "Daniel in der Löwengrube", "Simson", "Noahs Arche" }, 1),
            new CharadePrompt("Samson's Strength", "Simsons Kraft",
                new[] { "Strong", "Hair", "Lion", "Delilah" }, new[] { "Stark", "Haare", "Löwe", "Delila" },
                new[] { "Samson", "Goliath", "Hercules", "Moses" },
                new[] { "Simson", "Goliath", "Herkules", "Mose" }, 0),
            new CharadePrompt("The Good Samaritan", "Der barmherzige Samariter",
                new[] { "Road", "Help", "Robbers", "Samaritan", "Injured" }, new[] { "Straße", "Helfen", "Räuber", "Samariter", "Verletzt" },
                new[] { "The Good Samaritan", "The Prodigal Son", "The Lost Sheep", "The Sower" },
                new[] { "Der barmherzige Samariter", "Der verlorene Sohn", "Das verlorene Schaf", "Der Sämann" }, 0),
            new CharadePrompt("The Prodigal Son", "Der verlorene Sohn",
                new[] { "Son", "Father", "Return", "Forgive", "Inheritance" }, new[] { "Sohn", "Vater", "Rückkehr", "Vergeben", "Erbe" },
                new[] { "The Prodigal Son", "The Good Samaritan", "Cain and Abel", "Joseph and His Brothers" },
                new[] { "Der verlorene Sohn", "Der barmherzige Samariter", "Kain und Abel", "Josef und seine Brüder" }, 0),
            new CharadePrompt("Adam and Eve", "Adam und Eva",
                new[] { "Garden", "Apple", "Snake", "Eden", "Fruit" }, new[] { "Garten", "Apfel", "Schlange", "Eden", "Frucht" },
                new[] { "Adam and Eve", "Noah's Ark", "The Tower of Babel", "Cain and Abel" },
                new[] { "Adam und Eva", "Noahs Arche", "Der Turmbau zu Babel", "Kain und Abel" }, 0),
            new CharadePrompt("The Tower of Babel", "Der Turmbau zu Babel",
                new[] { "Tower", "Language", "Babel", "Build", "Confuse" }, new[] { "Turm", "Sprache", "Babel", "Bauen", "Verwirren" },
                new[] { "The Tower of Babel", "Noah's Ark", "The Exodus", "Jericho" },
                new[] { "Der Turmbau zu Babel", "Noahs Arche", "Der Auszug aus Ägypten", "Jericho" }, 0),
        };
    }
}
