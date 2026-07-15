using System.Collections.Generic;

namespace JesBox.Net
{
    // Microgames mode was cut (see MicrogameDef in Legacy/Microgames.cs) —
    // these payload shapes are kept here in case it's ever brought back.
    // GameManager.cs no longer sends either of these.

    public class MicrogamePayload
    {
        public string phase = "microgame";
        public int index;
        public int total;
        public string kind;
        public string title;
        public string instructions;
        public float duration;
    }

    public class MicrogameRevealPayload
    {
        public string phase = "microgame_reveal";
        public string title;
        public List<PlayerPublic> players;
    }
}
