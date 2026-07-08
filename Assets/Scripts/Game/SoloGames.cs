using System.Collections.Generic;

namespace JesBox.Game
{
    public enum SoloGameKind
    {
        FieryFurnaceDash,
        DavidsSlingshot,
        JoyfulPrayer,
        LoavesAndFishesMultiply,
        PartingTheSea
    }

    /// <summary>
    /// Unlike <see cref="MicrogameDef"/>, these render on the TV/host itself —
    /// the chosen player's phone is just a controller (WarioWare style). Every
    /// one is a single quick pass/fail challenge, not a scored timer.
    /// </summary>
    public class SoloGameDef
    {
        public SoloGameKind Kind;
        public string TitleEn, TitleDe;
        public string ControllerInstructionsEn, ControllerInstructionsDe;
        public float Duration;

        public string Title => L.Current == Language.German ? TitleDe : TitleEn;
        public string ControllerInstructions => L.Current == Language.German ? ControllerInstructionsDe : ControllerInstructionsEn;

        public SoloGameDef(SoloGameKind kind, string titleEn, string titleDe,
            string controllerInstructionsEn, string controllerInstructionsDe, float duration)
        {
            Kind = kind;
            TitleEn = titleEn;
            TitleDe = titleDe;
            ControllerInstructionsEn = controllerInstructionsEn;
            ControllerInstructionsDe = controllerInstructionsDe;
            Duration = duration;
        }
    }

    public static class SoloGames
    {
        public static readonly List<SoloGameDef> All = new List<SoloGameDef>
        {
            new SoloGameDef(SoloGameKind.FieryFurnaceDash, "Fiery Furnace Dash", "Feuerofen-Sprint",
                "Tap LEFT/RIGHT to dodge 3 flames — one hit and you're out!",
                "Tippe LINKS/RECHTS, um 3 Flammen auszuweichen — ein Treffer und du bist raus!", 6f),
            new SoloGameDef(SoloGameKind.DavidsSlingshot, "David's Slingshot", "Davids Schleuder",
                "You get ONE shot — tap FIRE when your sling lines up with Goliath!",
                "Du hast EINEN Schuss — tippe auf FEUER, wenn deine Schleuder auf Goliath zielt!", 4f),
            new SoloGameDef(SoloGameKind.JoyfulPrayer, "Joyful Prayer", "Freudiges Gebet",
                "Shake your phone as fast as you can before time runs out!",
                "Schüttle dein Handy so schnell du kannst, bevor die Zeit abläuft!", 3f),
            new SoloGameDef(SoloGameKind.LoavesAndFishesMultiply, "Loaves and Fishes", "Brote und Fische",
                "You get ONE try — tap MULTIPLY! the instant the basket lines up!",
                "Du hast EINEN Versuch — tippe auf VERMEHREN!, sobald der Korb ausgerichtet ist!", 4f),
            new SoloGameDef(SoloGameKind.PartingTheSea, "Parting the Sea", "Die Teilung des Meeres",
                "Tap LEFT, RIGHT, LEFT, RIGHT — fast! — to part the waters!",
                "Tippe LINKS, RECHTS, LINKS, RECHTS — schnell! —, um das Wasser zu teilen!", 4f),
        };
    }
}
