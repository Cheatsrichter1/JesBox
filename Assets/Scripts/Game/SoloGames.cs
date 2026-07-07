using System.Collections.Generic;

namespace JesBox.Game
{
    public enum SoloGameKind
    {
        FieryFurnaceDash,
        DavidsSlingshot
    }

    /// <summary>
    /// Unlike <see cref="MicrogameDef"/>, these render on the TV/host itself —
    /// the chosen player's phone is just a controller (WarioWare style).
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
                "Tap LEFT or RIGHT to dodge the flames!", 10f),
            new SoloGameDef(SoloGameKind.DavidsSlingshot, "David's Slingshot",
                "Tap FIRE the instant your sling lines up with Goliath!", 12f),
        };
    }
}
