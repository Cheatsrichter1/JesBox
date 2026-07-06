using System.Collections.Generic;

namespace JesBox.Game
{
    public enum MicrogameKind
    {
        MannaRain,
        FishersOfMen,
        JoyfulNoise,
        WalkOnWater,
        LoavesAndFishes,
        PartingWaters
    }

    public class MicrogameDef
    {
        public MicrogameKind Kind;
        public string Title;
        public string Instructions;
        public float Duration;

        /// <summary>
        /// True for games where the phone fires a "tap" action on every input
        /// (host tallies a running count). False for games that run their own
        /// gameplay client-side and report one final "submit_score" value.
        /// </summary>
        public bool UsesTapCounting;

        public MicrogameDef(MicrogameKind kind, string title, string instructions, float duration, bool usesTapCounting = false)
        {
            Kind = kind;
            Title = title;
            Instructions = instructions;
            Duration = duration;
            UsesTapCounting = usesTapCounting;
        }
    }

    public static class Microgames
    {
        public static readonly List<MicrogameDef> All = new List<MicrogameDef>
        {
            new MicrogameDef(MicrogameKind.MannaRain, "Manna Rain",
                "Tap as fast as you can to gather manna before time runs out!", 6f, usesTapCounting: true),
            new MicrogameDef(MicrogameKind.FishersOfMen, "Fishers of Men",
                "Tap the fish, avoid the tridents!", 8f),
            new MicrogameDef(MicrogameKind.JoyfulNoise, "Joyful Noise",
                "Shake your phone like a tambourine to make a joyful noise!", 6f, usesTapCounting: true),
            new MicrogameDef(MicrogameKind.WalkOnWater, "Walk on Water",
                "Tilt or drag left and right to dodge the waves — don't get soaked!", 8f),
            new MicrogameDef(MicrogameKind.LoavesAndFishes, "Loaves and Fishes",
                "Tap MULTIPLY! the instant the basket lines up with the target!", 7f),
            new MicrogameDef(MicrogameKind.PartingWaters, "Parting the Waters",
                "Swipe left, then right, then left again to part the sea!", 6f),
        };
    }
}
