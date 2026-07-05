using System.Collections.Generic;

namespace JesBox.Game
{
    public class VotePrompt
    {
        public string Scenario;
        public string[] Options;

        public VotePrompt(string scenario, string[] options)
        {
            Scenario = scenario;
            Options = options;
        }
    }

    public static class VotePrompts
    {
        public static readonly List<VotePrompt> All = new List<VotePrompt>
        {
            new VotePrompt("You see someone struggling to carry groceries up the stairs. What do you do?",
                new[] { "Offer to help carry them", "Hold the door and cheer them on", "Pretend to check your phone", "Ask if they've considered fewer groceries" }),
            new VotePrompt("A new kid sits alone at lunch. What's the most Christ-like move?",
                new[] { "Invite them to sit with you", "Wave from across the room", "Send a friendly text later", "Organize a whole welcome committee" }),
            new VotePrompt("Someone cuts you off in the church parking lot. You...",
                new[] { "Let it go and pray for them", "Give a slow, deliberate wave", "Practice your best patient sigh", "Tell the story dramatically at dinner" }),
            new VotePrompt("Your friend is venting about a hard week. Best response?",
                new[] { "Just listen and be present", "Offer to pray with them", "Send an encouraging verse", "Bring snacks, obviously" }),
            new VotePrompt("The offering plate is passed and you forgot your wallet. You...",
                new[] { "Offer a prayer instead", "Pass it along with a smile", "Promise to double up next week", "Discreetly Venmo the pastor" }),
            new VotePrompt("You find a wallet full of cash on the sidewalk. You...",
                new[] { "Track down the owner", "Turn it in to the nearest store", "Post about it on social media first", "Ask what Jesus would Venmo" }),
            new VotePrompt("It's your turn to say grace and your mind goes blank. You...",
                new[] { "Keep it short and sincere", "Stall with a very long 'ummm'", "Just repeat what your grandma always says", "Give a full sermon nobody asked for" }),
            new VotePrompt("A friend asks you to go to church with them for the first time. You...",
                new[] { "Say yes right away", "Ask a hundred questions first", "Say yes but show up 20 minutes late", "Bring snacks to share afterward" }),
        };
    }
}
