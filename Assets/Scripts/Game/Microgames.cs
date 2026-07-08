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
        public string TitleEn, TitleDe;
        public string InstructionsEn, InstructionsDe;
        public float Duration;

        public string Title => L.Current == Language.German ? TitleDe : TitleEn;
        public string Instructions => L.Current == Language.German ? InstructionsDe : InstructionsEn;

        /// <summary>
        /// True for games where the phone fires a "tap" action on every input
        /// (host tallies a running count). False for games that run their own
        /// gameplay client-side and report one final "submit_score" value.
        /// </summary>
        public bool UsesTapCounting;

        public MicrogameDef(MicrogameKind kind, string titleEn, string titleDe, string instructionsEn, string instructionsDe,
            float duration, bool usesTapCounting = false)
        {
            Kind = kind;
            TitleEn = titleEn;
            TitleDe = titleDe;
            InstructionsEn = instructionsEn;
            InstructionsDe = instructionsDe;
            Duration = duration;
            UsesTapCounting = usesTapCounting;
        }
    }

    public static class Microgames
    {
        public static readonly List<MicrogameDef> All = new List<MicrogameDef>
        {
            new MicrogameDef(MicrogameKind.MannaRain, "Manna Rain", "Manna-Regen",
                "Tap as fast as you can to gather manna before time runs out!",
                "Tippe so schnell du kannst, um Manna zu sammeln, bevor die Zeit abläuft!", 6f, usesTapCounting: true),
            new MicrogameDef(MicrogameKind.FishersOfMen, "Fishers of Men", "Menschenfischer",
                "Tap the fish, avoid the tridents!",
                "Tippe auf die Fische, meide die Dreizacke!", 8f),
            new MicrogameDef(MicrogameKind.JoyfulNoise, "Joyful Noise", "Freudiger Lärm",
                "Shake your phone like a tambourine to make a joyful noise!",
                "Schüttle dein Handy wie ein Tamburin für einen freudigen Lärm!", 6f, usesTapCounting: true),
            new MicrogameDef(MicrogameKind.WalkOnWater, "Walk on Water", "Wandeln auf dem Wasser",
                "Tilt or drag left and right to dodge the waves — don't get soaked!",
                "Neige oder ziehe nach links und rechts, um den Wellen auszuweichen — werde nicht nass!", 8f),
            new MicrogameDef(MicrogameKind.LoavesAndFishes, "Loaves and Fishes", "Brote und Fische",
                "Tap MULTIPLY! the instant the basket lines up with the target!",
                "Tippe auf VERMEHREN!, sobald der Korb mit dem Ziel übereinstimmt!", 7f),
            new MicrogameDef(MicrogameKind.PartingWaters, "Parting the Waters", "Teilung des Meeres",
                "Swipe left, then right, then left again to part the sea!",
                "Wische links, dann rechts, dann wieder links, um das Meer zu teilen!", 6f),
        };
    }
}
