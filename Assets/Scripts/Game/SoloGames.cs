using System.Collections.Generic;

namespace JesBox.Game
{
    public enum SoloGameKind
    {
        FieryFurnaceDash,
        DavidsSlingshot,
        JoyfulPrayer,
        LoavesAndFishesMultiply,
        PartingTheSea,
        SketchThatVerse
    }

    /// <summary>
    /// Unlike <see cref="MicrogameDef"/>, these render on the TV/host itself —
    /// the chosen player's phone is just a controller (WarioWare style). Every
    /// one is a single quick pass/fail challenge, not a scored timer.
    /// <see cref="SoloGameKind.SketchThatVerse"/> is the one exception — it has
    /// its own two-phase (draw, then guess) flow in GameManager instead of a
    /// simple pass/fail, so its <see cref="Duration"/> here is just the draw
    /// phase's length.
    /// </summary>
    public class SoloGameDef
    {
        public SoloGameKind Kind;
        public string Title;
        public string ControllerInstructions;
        public float Duration;

        public SoloGameDef(SoloGameKind kind, string title, string controllerInstructions, float duration)
        {
            Kind = kind;
            Title = title;
            ControllerInstructions = controllerInstructions;
            Duration = duration;
        }
    }

    public static class SoloGames
    {
        public static readonly List<SoloGameDef> All = new List<SoloGameDef>
        {
            new SoloGameDef(SoloGameKind.FieryFurnaceDash, "Fiery Furnace Dash",
                "Tap LEFT/RIGHT to dodge 3 flames — one hit and you're out!", 6f),
            new SoloGameDef(SoloGameKind.DavidsSlingshot, "David's Slingshot",
                "You get ONE shot — tap FIRE when your sling lines up with Goliath!", 4f),
            new SoloGameDef(SoloGameKind.JoyfulPrayer, "Joyful Prayer",
                "Shake your phone as fast as you can before time runs out!", 3f),
            new SoloGameDef(SoloGameKind.LoavesAndFishesMultiply, "Loaves and Fishes",
                "You get ONE try — tap MULTIPLY! the instant the basket lines up!", 4f),
            new SoloGameDef(SoloGameKind.PartingTheSea, "Parting the Sea",
                "Tap LEFT, RIGHT, LEFT, RIGHT — fast! — to part the waters!", 4f),
            new SoloGameDef(SoloGameKind.SketchThatVerse, "Sketch That Verse",
                "Draw the answer on your phone — everyone else has to guess it!", 20f),
        };
    }
}
