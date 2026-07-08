using System.Collections.Generic;

namespace JesBox.Game
{
    public class VotePrompt
    {
        public string ScenarioEn, ScenarioDe;
        public string[] OptionsEn, OptionsDe;

        public string Scenario => L.Current == Language.German ? ScenarioDe : ScenarioEn;
        public string[] Options => L.Current == Language.German ? OptionsDe : OptionsEn;

        public VotePrompt(string scenarioEn, string scenarioDe, string[] optionsEn, string[] optionsDe)
        {
            ScenarioEn = scenarioEn;
            ScenarioDe = scenarioDe;
            OptionsEn = optionsEn;
            OptionsDe = optionsDe;
        }
    }

    public static class VotePrompts
    {
        public static readonly List<VotePrompt> All = new List<VotePrompt>
        {
            new VotePrompt(
                "You see someone struggling to carry groceries up the stairs. What do you do?",
                "Du siehst jemanden, der Mühe hat, Einkäufe die Treppe hochzutragen. Was tust du?",
                new[] { "Offer to help carry them", "Hold the door and cheer them on", "Pretend to check your phone", "Ask if they've considered fewer groceries" },
                new[] { "Anbieten zu helfen", "Die Tür aufhalten und anfeuern", "So tun, als würdest du aufs Handy schauen", "Fragen, ob weniger Einkäufe nicht einfacher wären" }),
            new VotePrompt(
                "A new kid sits alone at lunch. What's the most Christ-like move?",
                "Ein neues Kind sitzt allein beim Mittagessen. Was wäre am christlichsten?",
                new[] { "Invite them to sit with you", "Wave from across the room", "Send a friendly text later", "Organize a whole welcome committee" },
                new[] { "Es einladen, sich zu dir zu setzen", "Von der anderen Seite des Raums winken", "Später eine freundliche Nachricht schicken", "Gleich ein ganzes Willkommenskomitee organisieren" }),
            new VotePrompt(
                "Someone cuts you off in the church parking lot. You...",
                "Jemand schneidet dich auf dem Kirchenparkplatz. Du...",
                new[] { "Let it go and pray for them", "Give a slow, deliberate wave", "Practice your best patient sigh", "Tell the story dramatically at dinner" },
                new[] { "Lässt es gut sein und betest für die Person", "Winkst langsam und betont", "Übst deinen geduldigsten Seufzer", "Erzählst die Geschichte beim Abendessen ganz dramatisch" }),
            new VotePrompt(
                "Your friend is venting about a hard week. Best response?",
                "Dein Freund lässt Dampf über eine harte Woche ab. Beste Reaktion?",
                new[] { "Just listen and be present", "Offer to pray with them", "Send an encouraging verse", "Bring snacks, obviously" },
                new[] { "Einfach zuhören und da sein", "Anbieten, gemeinsam zu beten", "Einen aufmunternden Bibelvers schicken", "Natürlich Snacks mitbringen" }),
            new VotePrompt(
                "The offering plate is passed and you forgot your wallet. You...",
                "Der Opferteller geht rum und du hast dein Portemonnaie vergessen. Du...",
                new[] { "Offer a prayer instead", "Pass it along with a smile", "Promise to double up next week", "Discreetly Venmo the pastor" },
                new[] { "Betest stattdessen", "Gibst ihn lächelnd weiter", "Versprichst, nächste Woche doppelt zu geben", "Überweist dem Pastor heimlich per PayPal" }),
            new VotePrompt(
                "You find a wallet full of cash on the sidewalk. You...",
                "Du findest ein Portemonnaie voller Bargeld auf dem Gehweg. Du...",
                new[] { "Track down the owner", "Turn it in to the nearest store", "Post about it on social media first", "Ask what Jesus would Venmo" },
                new[] { "Suchst den Besitzer", "Gibst es im nächsten Laden ab", "Postest erst mal in den sozialen Medien darüber", "Fragst dich, was Jesus wohl überweisen würde" }),
            new VotePrompt(
                "It's your turn to say grace and your mind goes blank. You...",
                "Du bist dran, das Tischgebet zu sprechen, und dein Kopf ist leer. Du...",
                new[] { "Keep it short and sincere", "Stall with a very long 'ummm'", "Just repeat what your grandma always says", "Give a full sermon nobody asked for" },
                new[] { "Hältst es kurz und ehrlich", "Überbrückst mit einem sehr langen 'ähm'", "Wiederholst einfach, was Oma immer sagt", "Hältst eine ganze Predigt, die niemand wollte" }),
            new VotePrompt(
                "A friend asks you to go to church with them for the first time. You...",
                "Ein Freund fragt dich, ob du zum ersten Mal mit in die Kirche kommst. Du...",
                new[] { "Say yes right away", "Ask a hundred questions first", "Say yes but show up 20 minutes late", "Bring snacks to share afterward" },
                new[] { "Sagst sofort zu", "Stellst erst hundert Fragen", "Sagst zu, kommst aber 20 Minuten zu spät", "Bringst hinterher Snacks zum Teilen mit" }),
        };
    }
}
