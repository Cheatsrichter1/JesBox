using System.Collections.Generic;

namespace JesBox.Game
{
    public enum MicrogameKind
    {
        MannaRain,
        FishersOfMen
    }

    public class MicrogameDef
    {
        public MicrogameKind Kind;
        public string Title;
        public string Instructions;
        public float Duration;

        public MicrogameDef(MicrogameKind kind, string title, string instructions, float duration)
        {
            Kind = kind;
            Title = title;
            Instructions = instructions;
            Duration = duration;
        }
    }

    public static class Microgames
    {
        public static readonly List<MicrogameDef> All = new List<MicrogameDef>
        {
            new MicrogameDef(MicrogameKind.MannaRain, "Manna Rain",
                "Tap as fast as you can to gather manna before time runs out!", 6f),
            new MicrogameDef(MicrogameKind.FishersOfMen, "Fishers of Men",
                "Tap the fish, avoid the tridents!", 8f),
        };
    }
}
