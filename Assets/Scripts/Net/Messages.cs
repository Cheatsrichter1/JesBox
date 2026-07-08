using System.Collections.Generic;

namespace JesBox.Net
{
    // ---- Inbound (server -> host) ----

    /// <summary>Minimal shape used only to sniff the "type" field first.</summary>
    public class EnvelopeIn
    {
        public string type;
    }

    public class RoomCreatedIn
    {
        public string type;
        public string roomCode;
    }

    public class PlayerJoinedIn
    {
        public string type;
        public string playerId;
        public string name;
    }

    public class PlayerLeftIn
    {
        public string type;
        public string playerId;
    }

    /// <summary>
    /// Generic per-player input. "action" says what kind of input this is;
    /// only the fields relevant to that action are populated by the phone.
    /// </summary>
    public class ActionData
    {
        public string action;
        public int choice = -1;
        public int value;
        public float x;
        public float y;
        public bool newStroke;
        public int colorIndex;
        public int brushSize;
    }

    public class GameIn
    {
        public string type;
        public string playerId;
        public string name;
        public ActionData data;
    }

    // ---- Outbound (host -> server) ----

    public class CreateRoomOut
    {
        public string type = "create_room";
    }

    public class GameOut<T>
    {
        public string type = "game";
        public T data;
    }

    /// <summary>Relayed to exactly one player instead of the whole room — for
    /// data (like a secret answer) that other players shouldn't receive at all.</summary>
    public class GameToOut<T>
    {
        public string type = "game_to";
        public string playerId;
        public T data;
    }

    public class PlayerPublic
    {
        public string id;
        public string name;
        public int score;
        public int delta;
    }

    public class LobbyPayload
    {
        public string phase = "lobby";
        public List<PlayerPublic> players;
    }

    // Trivia quiz
    public class QuestionPayload
    {
        public string phase = "question";
        public int index;
        public int total;
        public string question;
        public List<string> choices;
        public float timeLimit;
    }

    public class RevealPayload
    {
        public string phase = "reveal";
        public int correctIndex;
        public List<PlayerPublic> players;
    }

    // Microgames
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

    // Prompt & Vote
    public class VotePromptPayload
    {
        public string phase = "vote_prompt";
        public int index;
        public int total;
        public string scenario;
        public List<string> options;
        public float timeLimit;
    }

    public class VoteRevealPayload
    {
        public string phase = "vote_reveal";
        public List<int> tally;
        public int favoriteIndex;
        public List<PlayerPublic> players;
    }

    public class FinalPayload
    {
        public string phase = "final";
        public List<PlayerPublic> players;
    }

    // Chosen One (solo spotlight: one random player controls, everyone watches the TV)
    public class SoloTurnPayload
    {
        public string phase = "solo_turn";
        public int index;
        public int total;
        public string chosenId;
        public string chosenName;
        public string kind;
        public string title;
        public string controllerInstructions;
        public float duration;
    }

    public class SoloRevealPayload
    {
        public string phase = "solo_reveal";
        public string title;
        public List<PlayerPublic> players;
    }

    // Sketch & Guess: draw phase broadcast to everyone, the secret answer
    // sent only to the artist via GameToOut, then a guess phase broadcast to
    // everyone before falling back to SoloRevealPayload.
    public class SketchDrawPayload
    {
        public string phase = "sketch_draw";
        public int index;
        public int total;
        public string chosenId;
        public string chosenName;
        public float duration;
    }

    public class SketchAnswerPayload
    {
        public string phase = "sketch_answer";
        public string answer;
    }

    public class SketchGuessPayload
    {
        public string phase = "sketch_guess";
        public int index;
        public int total;
        public string chosenId;
        public string chosenName;
        public List<string> choices;
        public float timeLimit;
    }
}
