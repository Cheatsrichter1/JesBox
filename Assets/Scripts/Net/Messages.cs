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

    public class AnswerData
    {
        public string action;
        public int choice;
    }

    public class GameIn
    {
        public string type;
        public string playerId;
        public string name;
        public AnswerData data;
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

    public class FinalPayload
    {
        public string phase = "final";
        public List<PlayerPublic> players;
    }
}
