using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JesBox.Net;
using JesBox.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace JesBox.Game
{
    /// <summary>
    /// The TV-side host: owns the room, lets the host pick a game mode and its
    /// settings, runs whichever state machine that mode needs, and builds its
    /// own UI at runtime. Drop this on a single empty GameObject in the scene
    /// and press Play — no other scene setup required.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Connection")]
        [SerializeField] private string serverUrl = "ws://localhost:8080/ws";

        [Header("Trivia defaults")]
        [SerializeField] private float questionTimeLimit = 10f;
        [SerializeField] private int questionsPerGame = 5;
        [SerializeField] private float revealDuration = 5f;

        private enum GameMode { Trivia, Microgames, PromptVote, ChosenOne, Sketch, Charades }
        private enum CharadeType { Act, Describe }

        private class PlayerState
        {
            public string Name;
            public int Score;
            /// <summary>True while the server is holding this player's slot
            /// open during the post-disconnect grace period.</summary>
            public bool Disconnected;
        }

        private struct Answer
        {
            public int Choice;
            public float RemainingTime;
        }

        private NetworkClient _net;
        private readonly Dictionary<string, PlayerState> _players = new Dictionary<string, PlayerState>();
        private readonly Dictionary<string, Answer> _answersThisRound = new Dictionary<string, Answer>();
        private readonly Dictionary<string, int> _tapCounts = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _submittedScores = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _votesThisRound = new Dictionary<string, int>();
        private string _roomCode = "----";
        private bool _gameRunning;
        private float _currentRemaining;

        // Reconnection: the most recent room-wide broadcast (and any per-player
        // targeted secret) so a phone that rejoins mid-round can be caught up
        // instantly instead of sitting on a stale screen until the next phase.
        private string _lastBroadcastJson;
        private readonly Dictionary<string, string> _lastTargetedJson = new Dictionary<string, string>();

        // Host controls: pause freezes every round's timer (Dt() returns 0
        // while paused), skip force-ends whichever round loop is currently
        // running, and end-game additionally stops the outer per-mode loop
        // from starting another round, falling through to FinishGame().
        private bool _paused;
        private bool _skipRequested;
        private bool _endGameRequested;

        private float Dt() => _paused ? 0f : Time.deltaTime;

        // Host-selected settings
        private GameMode _selectedMode = GameMode.Trivia;
        private Difficulty _selectedDifficulty = Difficulty.Medium;
        private int _microgameRounds = 4;
        private int _votePromptCount = 5;
        private int _soloTurns = 6;
        private int _sketchRounds = 5;
        private int _charadeRounds = 6;

        // Chosen One (solo spotlight) live state — every game here is a single
        // quick pass/fail challenge (WarioWare style), not a scored timer.
        private class SoloObstacle
        {
            public RectTransform Rt;
            public int Lane;
        }

        private string _currentChosenId;
        private SoloGameKind _currentSoloKind;
        private bool _soloRoundOver;
        private bool _soloWon;
        private bool _inSketchTurn;
        // Scales up each Chosen One turn (fall/target speed, shrinking duration)
        // so the whole mode ramps up in intensity like a real WarioWare set.
        private float _soloIntensity = 1f;
        private const float SoloIntroDuration = 0.55f;
        private const float SoloStampDuration = 0.5f;

        // Fiery Furnace Dash
        private int _soloLane;
        private int _soloHits;
        private int _soloDodged;
        private float _soloSpawnTimer;
        private RectTransform _soloPlayerMarker;
        private readonly List<SoloObstacle> _soloObstacles = new List<SoloObstacle>();
        private const int SoloDodgeTarget = 3;
        private const float SoloLaneOffset = 280f;
        private const float SoloPlayerY = -180f;
        private const float SoloSpawnY = 180f;
        private const float SoloFurnaceFallSpeed = 340f;
        private const float SoloFurnaceSpawnGap = 0.25f;

        // David's Slingshot / Loaves and Fishes Multiply (one-shot moving-target games)
        private float _soloTargetTime;
        private float _soloTargetX;
        private RectTransform _soloTargetMarker;
        private const float SoloTargetAmplitude = 320f;
        private const float SoloTargetHitTolerance = 70f;

        // Joyful Prayer
        private int _soloPrayerCount;
        private const int SoloPrayerTarget = 8;

        // Parting the Sea
        private int _soloPartingCount;
        private int _soloPartingLastDir;
        private const int SoloPartingTarget = 8;
        private const float SoloPartingSteerThreshold = 0.5f;

        // Sketch That Verse (draw phase renders live strokes from the artist's
        // phone onto _soloStage; guess phase reuses _answersThisRound). The
        // stage is temporarily resized bigger than Chosen One's default while
        // a sketch turn is running — see the size/position resets at the top
        // of RunSoloTurn and RunSketchTurn.
        private Vector2? _lastDrawPoint;
        private readonly List<RectTransform> _drawSegments = new List<RectTransform>();
        // Chosen One games fill almost the entire 1920x1080 canvas,
        // WarioWare-style — the prompt/title only shows briefly at the start
        // (SetSoloPromptVisible), and the timer (added after the stage, so it
        // always renders on top) sits right at the bottom edge as a HUD
        // overlay rather than in its own reserved strip.
        private static readonly Vector2 SoloStageDefaultSize = new Vector2(1910f, 1060f);
        private static readonly Vector2 SoloStageDefaultPos = new Vector2(0f, 0f);
        private static readonly Vector2 SketchStageSize = new Vector2(900f, 480f);
        private static readonly Vector2 SketchStagePos = new Vector2(0f, -10f);
        private const float SketchGuessDuration = 10f;
        private const int SketchGuesserPoints = 500;
        private const int SketchArtistPointsPerGuesser = 150;

        private const float SoloRevealDuration = 2.2f;

        // Bible Charades: same broadcast/secret/guess/reveal shape as Sketch &
        // Guess (see RunSketchTurn), but nothing streams to the TV — the
        // performance happens live in the room, and the chosen player's phone
        // just displays the secret.
        private const float CharadeDuration = 45f;
        private const float CharadeGuessDuration = 10f;
        private const int CharadeGuesserPoints = 500;
        private const int CharadeArtistPointsPerGuesser = 150;

        // Sound
        private SoundManager _sound;
        private int _lastCountdownBeepSecond = -1;

        // UI references
        private Transform _canvasRoot;
        private RectTransform _lobbyPanel, _questionPanel, _microgamePanel, _revealPanel, _finalPanel, _soloPanel;
        private RectTransform _triviaSettingsGroup, _microgameSettingsGroup, _voteSettingsGroup, _soloSettingsGroup, _sketchSettingsGroup, _charadeSettingsGroup;
        private Text _lobbyCodeText, _lobbyPlayersText;
        private Button _startButton;
        private Dictionary<int, Button> _modeChips;
        private Dictionary<int, Button> _difficultyChips;
        private Dictionary<int, Button> _languageChips;
        private RawImage _qrImage;
        private Texture2D _qrTexture;
        private Text _questionHeaderText, _questionBodyText, _questionChoicesText, _questionTimerText;
        private Image _questionTimerFill;
        private Text _microgameHeaderText, _microgameTitleText, _microgameInstructionsText, _microgameTimerText;
        private Image _microgameTimerFill;
        private Text _revealBannerText, _revealScoresText;
        private Text _finalScoresText;
        private Button _backToMenuButton;
        private Text _soloHeaderText, _soloChosenNameText, _soloTitleText, _soloInstructionsText, _soloTimerText;
        private Image _soloTimerFill;
        private RectTransform _soloStage;
        private ISoloGameVisual _soloVisual;
        private Text _soloVerbText;
        private Image _flashOverlay;
        private RectTransform _hostControlsBar;
        private Button _pauseButton, _skipButton, _endGameButton, _playersToggleButton;
        private Text _pauseButtonLabel;
        private RectTransform _playersOverlay;
        private RectTransform _playersListGroup;
        private bool _playersOverlayOpen;

        private void Awake()
        {
            _net = gameObject.AddComponent<NetworkClient>();
            _net.OnOpen += HandleOpen;
            _net.OnTextMessage += HandleMessage;
            _net.OnError += err => Debug.LogWarning($"[JesBox] Socket error: {err}");
            _net.OnClose += () => Debug.LogWarning("[JesBox] Socket closed.");

            _sound = gameObject.AddComponent<SoundManager>();

            EnsureEventSystem();
            BuildUI();
            ShowOnly(_lobbyPanel);
        }

        private void Start()
        {
            _net.Connect(serverUrl);
        }

        private void Update()
        {
            // Esc opens/closes the admin menu (Pause/Skip/End Game/Players),
            // which is otherwise hidden during actual gameplay so it doesn't
            // clutter the now-fullscreen games — see UpdateHostControlsUI().
            if (_gameRunning && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TogglePause();
            }
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private string JoinUrlHint()
        {
            string url = serverUrl;
            if (url.StartsWith("wss://")) url = "https://" + url.Substring(6);
            else if (url.StartsWith("ws://")) url = "http://" + url.Substring(5);
            int idx = url.IndexOf("/ws");
            if (idx >= 0) url = url.Substring(0, idx);
            return url;
        }

        // ---- QR code (join link, encoding roomCode as a ?code= query param
        // the phone's JoinScreen reads on load) ----

        private void ApplyOrFetchQr()
        {
            if (_qrImage == null) return;
            if (_qrTexture != null) { _qrImage.texture = _qrTexture; return; }
            if (_roomCode != "----") StartCoroutine(FetchQrCode($"{JoinUrlHint()}/?code={_roomCode}"));
        }

        private IEnumerator FetchQrCode(string joinUrl)
        {
            string apiUrl = "https://api.qrserver.com/v1/create-qr-code/?size=300x300&data=" + UnityWebRequest.EscapeURL(joinUrl);
            using (var req = UnityWebRequestTexture.GetTexture(apiUrl))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    _qrTexture = DownloadHandlerTexture.GetContent(req);
                    if (_qrImage != null) _qrImage.texture = _qrTexture;
                }
                else
                {
                    Debug.LogWarning($"[JesBox] QR code fetch failed: {req.error}");
                }
            }
        }

        // ---- Networking ----

        private void HandleOpen()
        {
            _net.SendJson(new CreateRoomOut());
        }

        private void HandleMessage(string raw)
        {
            EnvelopeIn envelope;
            try { envelope = JsonConvert.DeserializeObject<EnvelopeIn>(raw); }
            catch { return; }
            if (envelope == null || string.IsNullOrEmpty(envelope.type)) return;

            switch (envelope.type)
            {
                case "room_created":
                    _roomCode = JsonConvert.DeserializeObject<RoomCreatedIn>(raw).roomCode;
                    RefreshLobbyUI();
                    ApplyOrFetchQr();
                    break;

                case "player_joined":
                {
                    var joined = JsonConvert.DeserializeObject<PlayerJoinedIn>(raw);
                    _players[joined.playerId] = new PlayerState { Name = joined.name, Score = 0 };
                    RefreshLobbyUI();
                    BroadcastLobby();
                    if (_playersOverlayOpen) RefreshPlayersOverlay();
                    _sound.PlayJoin();
                    break;
                }

                case "player_left":
                {
                    var left = JsonConvert.DeserializeObject<PlayerLeftIn>(raw);
                    _players.Remove(left.playerId);
                    _lastTargetedJson.Remove(left.playerId);
                    RefreshLobbyUI();
                    BroadcastLobby();
                    if (_playersOverlayOpen) RefreshPlayersOverlay();
                    break;
                }

                case "player_disconnected":
                {
                    // Their slot (and score) is held open server-side for a
                    // grace period — nothing to do here but flag it so the
                    // host can see who's dropped.
                    var disconnected = JsonConvert.DeserializeObject<PlayerDisconnectedIn>(raw);
                    if (_players.TryGetValue(disconnected.playerId, out var droppedPlayer))
                        droppedPlayer.Disconnected = true;
                    RefreshLobbyUI();
                    BroadcastLobby();
                    if (_playersOverlayOpen) RefreshPlayersOverlay();
                    break;
                }

                case "player_reconnected":
                {
                    var reconnected = JsonConvert.DeserializeObject<PlayerReconnectedIn>(raw);
                    if (_players.TryGetValue(reconnected.playerId, out var backPlayer))
                        backPlayer.Disconnected = false;
                    RefreshLobbyUI();
                    BroadcastLobby();
                    if (_playersOverlayOpen) RefreshPlayersOverlay();
                    ResyncPlayer(reconnected.playerId);
                    _sound.PlayJoin();
                    break;
                }

                case "game":
                {
                    var game = JsonConvert.DeserializeObject<GameIn>(raw);
                    if (!_gameRunning || game?.data == null) break;

                    switch (game.data.action)
                    {
                        case "answer":
                            if (!_answersThisRound.ContainsKey(game.playerId))
                                _answersThisRound[game.playerId] = new Answer { Choice = game.data.choice, RemainingTime = _currentRemaining };
                            break;
                        case "tap":
                            _tapCounts.TryGetValue(game.playerId, out var taps);
                            _tapCounts[game.playerId] = taps + 1;
                            break;
                        case "submit_score":
                            _submittedScores[game.playerId] = game.data.value;
                            break;
                        case "vote":
                            if (!_votesThisRound.ContainsKey(game.playerId))
                                _votesThisRound[game.playerId] = game.data.choice;
                            break;
                        case "move":
                            if (game.playerId == _currentChosenId) HandleSoloMove(game.data.choice);
                            break;
                        case "fire":
                            if (game.playerId == _currentChosenId) HandleSoloFire();
                            break;
                        case "shake":
                            if (game.playerId == _currentChosenId) HandleSoloShake();
                            break;
                        case "steer":
                            if (game.playerId == _currentChosenId) HandleSoloSteer(game.data.x);
                            break;
                        case "draw_point":
                            if (game.playerId == _currentChosenId && _inSketchTurn)
                                HandleDrawPoint(game.data.x, game.data.y, game.data.newStroke, game.data.colorIndex, game.data.brushSize);
                            break;
                        case "draw_clear":
                            if (game.playerId == _currentChosenId && _inSketchTurn)
                                HandleDrawClear();
                            break;
                    }
                    break;
                }
            }
        }

        private List<PlayerPublic> PublicList(Dictionary<string, int> deltas = null)
        {
            return _players.Select(kv => new PlayerPublic
            {
                id = kv.Key,
                name = kv.Value.Name,
                score = kv.Value.Score,
                delta = deltas != null && deltas.TryGetValue(kv.Key, out var d) ? d : 0
            }).ToList();
        }

        private void BroadcastLobby()
        {
            BroadcastGame(new LobbyPayload { players = PublicList() });
        }

        // ---- Reconnection: resend state to a rejoined phone ----

        /// <summary>Sends <paramref name="data"/> to every player, same as
        /// wrapping it in a <see cref="GameOut{T}"/> — but also stashes the
        /// serialized JSON so a reconnecting player can be caught up later
        /// via <see cref="ResyncPlayer"/>.</summary>
        private void BroadcastGame<T>(T data)
        {
            _lastBroadcastJson = JsonConvert.SerializeObject(data);
            _net.SendRaw("{\"type\":\"game\",\"data\":" + _lastBroadcastJson + "}");
        }

        /// <summary>Sends <paramref name="data"/> to exactly one player, same
        /// as wrapping it in a <see cref="GameToOut{T}"/> — but also stashes
        /// it (keyed by player) as a secret to resend if that same player
        /// reconnects mid-turn (e.g. Sketch & Guess's answer, Bible Charades'
        /// prompt).</summary>
        private void SendToPlayer<T>(string playerId, T data)
        {
            string json = JsonConvert.SerializeObject(data);
            _lastTargetedJson[playerId] = json;
            SendGameToRaw(playerId, json);
        }

        private void SendGameToRaw(string playerId, string dataJson)
        {
            _net.SendRaw("{\"type\":\"game_to\",\"playerId\":" + JsonConvert.SerializeObject(playerId) + ",\"data\":" + dataJson + "}");
        }

        /// <summary>Catches a reconnected phone up on whatever's currently
        /// happening: the last room-wide broadcast (with its time-remaining
        /// field patched to the real current value, not the original full
        /// duration), plus that player's own secret if they're the one
        /// currently drawing/performing.</summary>
        private void ResyncPlayer(string playerId)
        {
            if (_lastBroadcastJson != null)
                SendGameToRaw(playerId, PatchRemainingTime(_lastBroadcastJson));
            if (_lastTargetedJson.TryGetValue(playerId, out var secretJson))
                SendGameToRaw(playerId, secretJson);
        }

        private string PatchRemainingTime(string json)
        {
            try
            {
                var obj = JObject.Parse(json);
                if (obj["timeLimit"] != null) obj["timeLimit"] = _currentRemaining;
                else if (obj["duration"] != null) obj["duration"] = _currentRemaining;
                return obj.ToString(Formatting.None);
            }
            catch
            {
                return json;
            }
        }

        // ---- Game flow: shared ----

        private void OnStartClicked()
        {
            if (_gameRunning || _players.Count == 0) return;
            _gameRunning = true;
            _paused = false;
            _skipRequested = false;
            _endGameRequested = false;
            _startButton.gameObject.SetActive(false);
            _sound.PlayRoundStart();
            UpdateHostControlsUI();

            switch (_selectedMode)
            {
                case GameMode.Trivia: StartCoroutine(RunTriviaGame()); break;
                case GameMode.Microgames: StartCoroutine(RunMicrogames()); break;
                case GameMode.PromptVote: StartCoroutine(RunPromptVote()); break;
                case GameMode.ChosenOne: StartCoroutine(RunChosenOne()); break;
                case GameMode.Sketch: StartCoroutine(RunSketchGame()); break;
                case GameMode.Charades: StartCoroutine(RunCharadesGame()); break;
            }
        }

        private void OnBackToMenuClicked()
        {
            foreach (var player in _players.Values) player.Score = 0;
            _gameRunning = false;
            _paused = false;
            _answersThisRound.Clear();
            _tapCounts.Clear();
            _submittedScores.Clear();
            _votesThisRound.Clear();
            _currentChosenId = null;
            _inSketchTurn = false;
            ClearSoloStage();
            RefreshLobbyUI();
            BroadcastLobby();
            UpdateHostControlsUI();
            ShowOnly(_lobbyPanel);
        }

        private void FinishGame()
        {
            var sorted = PublicList().OrderByDescending(p => p.score).ToList();
            BroadcastGame(new FinalPayload { players = sorted });
            ShowFinalUI(sorted);
            _gameRunning = false;
            _paused = false;
            UpdateHostControlsUI();
        }

        // ---- Host controls: pause/resume, skip round, end game, kick ----

        private void TogglePause()
        {
            if (!_gameRunning) return;
            _paused = !_paused;
            BroadcastGame(new PauseStatePayload { paused = _paused });
            UpdateHostControlsUI();
        }

        private void RequestSkip()
        {
            if (!_gameRunning) return;
            _skipRequested = true;
        }

        private void RequestEndGame()
        {
            if (!_gameRunning) return;
            _endGameRequested = true;
            _skipRequested = true; // also bail out of whatever round is live right now
        }

        private void KickPlayer(string playerId)
        {
            if (!_players.ContainsKey(playerId)) return;
            _net.SendJson(new KickPlayerOut { playerId = playerId });
            _players.Remove(playerId);
            _lastTargetedJson.Remove(playerId);
            RefreshLobbyUI();
            BroadcastLobby();
            RefreshPlayersOverlay();
        }

        private void TogglePlayersOverlay()
        {
            _playersOverlayOpen = !_playersOverlayOpen;
            _playersOverlay.gameObject.SetActive(_playersOverlayOpen);
            if (_playersOverlayOpen) RefreshPlayersOverlay();
        }

        /// <summary>The whole admin menu (Pause/Skip/End Game/Players) is
        /// hidden during actual gameplay — Esc (see Update()) is what reveals
        /// it, pausing the game at the same time. It's also always visible in
        /// the lobby, since Players (kicking) is a game-selection-time
        /// concern. Pause/Skip/End Game specifically only make sense once
        /// paused mid-game — there's nothing to skip/end from the lobby.</summary>
        private void UpdateHostControlsUI()
        {
            bool pausedMidGame = _gameRunning && _paused;
            _pauseButton.gameObject.SetActive(pausedMidGame);
            _skipButton.gameObject.SetActive(pausedMidGame);
            _endGameButton.gameObject.SetActive(pausedMidGame);

            bool adminMenuVisible = !_gameRunning || _paused;
            _playersToggleButton.gameObject.SetActive(adminMenuVisible);
            if (!adminMenuVisible && _playersOverlayOpen)
            {
                _playersOverlayOpen = false;
                _playersOverlay.gameObject.SetActive(false);
            }

            _pauseButtonLabel.text = _paused ? L.T("host.resume") : L.T("host.pause");
        }

        private static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // ---- Game flow: Trivia ----

        private IEnumerator RunTriviaGame()
        {
            var pool = TriviaQuestions.ForDifficulty(_selectedDifficulty);
            Shuffle(pool);
            int count = Mathf.Min(questionsPerGame, pool.Count);

            for (int i = 0; i < count && !_endGameRequested; i++)
            {
                yield return RunQuestion(pool[i], i, count);
            }

            FinishGame();
        }

        private IEnumerator RunQuestion(TriviaQuestion q, int index, int total)
        {
            _answersThisRound.Clear();
            _currentRemaining = questionTimeLimit;

            BroadcastGame(new QuestionPayload
            {
                index = index,
                total = total,
                question = q.Question,
                choices = new List<string>(q.Choices),
                timeLimit = questionTimeLimit
            });
            ShowQuestionUI(q, index, total);

            float elapsed = 0f;
            while (elapsed < questionTimeLimit && _answersThisRound.Count < _players.Count && !_skipRequested)
            {
                elapsed += Dt();
                _currentRemaining = Mathf.Max(0f, questionTimeLimit - elapsed);
                UpdateQuestionTimerUI();
                yield return null;
            }
            _skipRequested = false;

            var deltas = new Dictionary<string, int>();
            foreach (var kv in _players)
            {
                int delta = 0;
                if (_answersThisRound.TryGetValue(kv.Key, out var ans) && ans.Choice == q.CorrectIndex)
                {
                    delta = 700 + Mathf.RoundToInt(300f * (ans.RemainingTime / questionTimeLimit));
                }
                kv.Value.Score += delta;
                deltas[kv.Key] = delta;
            }

            var publicList = PublicList(deltas);
            BroadcastGame(new RevealPayload { correctIndex = q.CorrectIndex, players = publicList });
            string[] letters = { "A", "B", "C", "D" };
            ShowRevealUI(L.T("question.correctAnswer", letters[q.CorrectIndex], q.Choices[q.CorrectIndex]), publicList);

            yield return new WaitForSeconds(revealDuration);
        }

        // ---- Game flow: Microgames ----

        private IEnumerator RunMicrogames()
        {
            var defs = new List<MicrogameDef>(Microgames.All);
            Shuffle(defs);

            for (int i = 0; i < _microgameRounds && !_endGameRequested; i++)
            {
                yield return RunMicrogameRound(defs[i % defs.Count], i, _microgameRounds);
            }

            FinishGame();
        }

        private IEnumerator RunMicrogameRound(MicrogameDef def, int index, int total)
        {
            _tapCounts.Clear();
            _submittedScores.Clear();
            _currentRemaining = def.Duration;

            BroadcastGame(new MicrogamePayload
            {
                index = index,
                total = total,
                kind = def.Kind.ToString(),
                title = def.Title,
                instructions = def.Instructions,
                duration = def.Duration
            });
            ShowMicrogameUI(def, index, total);

            float elapsed = 0f;
            while (elapsed < def.Duration && !_skipRequested)
            {
                elapsed += Dt();
                _currentRemaining = Mathf.Max(0f, def.Duration - elapsed);
                UpdateMicrogameTimerUI(def.Duration);
                yield return null;
            }
            _skipRequested = false;

            // Small grace period for last-moment "submit_score" messages to arrive.
            yield return new WaitForSeconds(0.4f);

            int[] rankPoints = { 500, 350, 250, 150 };
            var ranked = _players.Keys
                .Select(id => new { id, raw = RawMicrogameScore(def, id) })
                .OrderByDescending(x => x.raw)
                .ToList();

            var deltas = new Dictionary<string, int>();
            for (int i = 0; i < ranked.Count; i++)
            {
                int points = ranked[i].raw > 0 ? rankPoints[Mathf.Min(i, rankPoints.Length - 1)] : 0;
                deltas[ranked[i].id] = points;
                _players[ranked[i].id].Score += points;
            }

            var publicList = PublicList(deltas);
            string microResultText = L.T("microgame.results", def.Title);
            BroadcastGame(new MicrogameRevealPayload { title = microResultText, players = publicList });
            ShowRevealUI(microResultText, publicList);

            yield return new WaitForSeconds(revealDuration);
        }

        private int RawMicrogameScore(MicrogameDef def, string playerId)
        {
            if (def.UsesTapCounting)
            {
                _tapCounts.TryGetValue(playerId, out var taps);
                return taps;
            }

            _submittedScores.TryGetValue(playerId, out var score);
            return score;
        }

        // ---- Game flow: Prompt & Vote ----

        private IEnumerator RunPromptVote()
        {
            var pool = new List<VotePrompt>(VotePrompts.All);
            Shuffle(pool);
            int count = Mathf.Min(_votePromptCount, pool.Count);

            for (int i = 0; i < count && !_endGameRequested; i++)
            {
                yield return RunVoteRound(pool[i], i, count);
            }

            FinishGame();
        }

        private IEnumerator RunVoteRound(VotePrompt prompt, int index, int total)
        {
            _votesThisRound.Clear();
            const float duration = 12f;
            _currentRemaining = duration;

            BroadcastGame(new VotePromptPayload
            {
                index = index,
                total = total,
                scenario = prompt.Scenario,
                options = new List<string>(prompt.Options),
                timeLimit = duration
            });
            ShowVotePromptUI(prompt, index, total);

            float elapsed = 0f;
            while (elapsed < duration && _votesThisRound.Count < _players.Count && !_skipRequested)
            {
                elapsed += Dt();
                _currentRemaining = Mathf.Max(0f, duration - elapsed);
                UpdateQuestionTimerUI();
                yield return null;
            }
            _skipRequested = false;

            var tally = new int[prompt.Options.Length];
            foreach (var choice in _votesThisRound.Values)
            {
                if (choice >= 0 && choice < tally.Length) tally[choice]++;
            }

            int maxVotes = tally.Length > 0 ? tally.Max() : 0;
            int favoriteIndex = -1;
            if (maxVotes > 0)
            {
                for (int i = 0; i < tally.Length; i++)
                {
                    if (tally[i] == maxVotes) { favoriteIndex = i; break; }
                }
            }

            var deltas = new Dictionary<string, int>();
            foreach (var kv in _players)
            {
                int delta = 0;
                if (_votesThisRound.TryGetValue(kv.Key, out var choice))
                {
                    delta = choice == favoriteIndex ? 500 : 100;
                }
                kv.Value.Score += delta;
                deltas[kv.Key] = delta;
            }

            var publicList = PublicList(deltas);
            BroadcastGame(new VoteRevealPayload { tally = new List<int>(tally), favoriteIndex = favoriteIndex, players = publicList });

            string[] letters = { "A", "B", "C", "D" };
            string banner = favoriteIndex >= 0
                ? L.T("vote.crowdFavorite", letters[favoriteIndex], prompt.Options[favoriteIndex])
                : L.T("vote.noVotes");
            ShowRevealUI(banner, publicList);

            yield return new WaitForSeconds(revealDuration);
        }

        // ---- Game flow: Chosen One (solo spotlight) ----

        private IEnumerator RunChosenOne()
        {
            var bag = new List<string>();

            for (int i = 0; i < _soloTurns && !_endGameRequested; i++)
            {
                string chosenId = PopNextChosenPlayer(bag);
                if (chosenId == null) break; // everyone left

                var def = SoloGames.All[Random.Range(0, SoloGames.All.Count)];
                yield return RunSoloTurn(def, chosenId, i, _soloTurns);
            }

            FinishGame();
        }

        // ---- Game flow: Sketch & Guess ----

        private IEnumerator RunSketchGame()
        {
            var bag = new List<string>();

            for (int i = 0; i < _sketchRounds && !_endGameRequested; i++)
            {
                string chosenId = PopNextChosenPlayer(bag);
                if (chosenId == null) break; // everyone left

                yield return RunSketchTurn(chosenId, i, _sketchRounds);
            }

            FinishGame();
        }

        private string PopNextChosenPlayer(List<string> bag)
        {
            while (true)
            {
                if (bag.Count == 0)
                {
                    if (_players.Count == 0) return null;
                    bag.AddRange(_players.Keys);
                    Shuffle(bag);
                }

                string id = bag[0];
                bag.RemoveAt(0);
                if (_players.ContainsKey(id)) return id;
            }
        }

        private IEnumerator RunSoloTurn(SoloGameDef def, string chosenId, int index, int total)
        {
            _currentChosenId = chosenId;
            _currentSoloKind = def.Kind;
            _soloRoundOver = false;
            _soloWon = false;
            _soloLane = 1;
            _soloHits = 0;
            _soloDodged = 0;
            _soloSpawnTimer = 0f;
            _soloTargetTime = 0f;
            _soloTargetX = 0f;
            _soloPrayerCount = 0;
            _soloPartingCount = 0;
            _soloPartingLastDir = 0;

            // Every turn ramps up a little — faster flames/targets and a
            // shrinking window, like a WarioWare set speeding up as it goes.
            _soloIntensity = Mathf.Min(1f + index * 0.15f, 1.9f);
            float duration = Mathf.Max(def.Duration * (1f - index * 0.06f), def.Duration * 0.65f);
            _currentRemaining = duration;

            string chosenName = _players[chosenId].Name;

            ShowSoloTurnUI(def, chosenName, index, total);
            _soloStage.anchoredPosition = SoloStageDefaultPos;
            _soloStage.sizeDelta = SoloStageDefaultSize;
            _soloStage.localScale = Vector3.one;
            SetupSoloStage(def.Kind);

            // Verb flash + stage punch-in beat — the stage is set up but not
            // yet ticking, and the phone hasn't been told to start yet either,
            // so this doesn't cost the player any reaction time.
            yield return PlaySoloIntro(def);

            // The prompt (title/instructions) only needs to be on screen for
            // that intro beat — hide it now so the stage has the whole
            // screen to itself while the round is actually playing.
            SetSoloPromptVisible(false);

            BroadcastGame(new SoloTurnPayload
            {
                index = index,
                total = total,
                chosenId = chosenId,
                chosenName = chosenName,
                kind = def.Kind.ToString(),
                title = def.Title,
                controllerInstructions = def.ControllerInstructions,
                verb = def.Verb,
                duration = duration
            });

            float elapsed = 0f;
            while (elapsed < duration && !_soloRoundOver && !_skipRequested)
            {
                elapsed += Dt();
                _currentRemaining = Mathf.Max(0f, duration - elapsed);
                UpdateSoloTimerUI(duration);
                TickSoloGame(def.Kind, Dt());
                yield return null;
            }
            _skipRequested = false;

            // Ran out of time without a hit — Fiery Furnace Dash counts that as
            // a survival. Every other game requires an explicit action to win.
            if (!_soloRoundOver && def.Kind == SoloGameKind.FieryFurnaceDash && _soloHits == 0)
            {
                _soloWon = true;
            }

            // Instant win/fail judgment stamp before cutting to the scoreboard.
            yield return PlaySoloStamp(_soloWon);

            ClearSoloStage();
            _currentChosenId = null;

            if (!_players.TryGetValue(chosenId, out var chosenPlayer))
            {
                // Chosen player disconnected mid-turn; skip scoring and move on.
                yield break;
            }

            int points = _soloWon ? 500 : 0;
            var deltas = new Dictionary<string, int>();
            foreach (var kv in _players) deltas[kv.Key] = 0;
            deltas[chosenId] = points;
            chosenPlayer.Score += points;

            var publicList = PublicList(deltas);
            string resultText = BuildSoloResultText(def.Kind, chosenName, points);
            BroadcastGame(new SoloRevealPayload { title = resultText, players = publicList });
            ShowRevealUI(resultText, publicList);

            yield return new WaitForSeconds(SoloRevealDuration);
        }

        private const float SketchDrawDuration = 20f;

        private IEnumerator RunSketchTurn(string chosenId, int index, int total)
        {
            _currentChosenId = chosenId;
            _inSketchTurn = true;
            _lastDrawPoint = null;
            string chosenName = _players[chosenId].Name;
            var prompt = DrawPrompts.All[Random.Range(0, DrawPrompts.All.Count)];

            // ---- Draw phase: broadcast the round to everyone, then send the
            // secret answer to just the artist via a targeted message.
            float drawDuration = SketchDrawDuration;
            _currentRemaining = drawDuration;

            BroadcastGame(new SketchDrawPayload { index = index, total = total, chosenId = chosenId, chosenName = chosenName, duration = drawDuration });
            SendToPlayer(chosenId, new SketchAnswerPayload { answer = prompt.Answer });

            ShowSketchDrawUI(chosenName, index, total);
            _soloStage.anchoredPosition = SketchStagePos;
            _soloStage.sizeDelta = SketchStageSize;
            ClearSoloStage();

            float elapsed = 0f;
            while (elapsed < drawDuration && !_skipRequested)
            {
                elapsed += Dt();
                _currentRemaining = Mathf.Max(0f, drawDuration - elapsed);
                UpdateSoloTimerUI(drawDuration);
                yield return null;
            }
            _skipRequested = false;

            // ---- Guess phase: everyone but the artist picks from 4 options.
            // Reuses _answersThisRound/"answer" — same shape as trivia guesses.
            _answersThisRound.Clear();
            _currentRemaining = SketchGuessDuration;

            BroadcastGame(new SketchGuessPayload
            {
                index = index,
                total = total,
                chosenId = chosenId,
                chosenName = chosenName,
                choices = new List<string>(prompt.Choices),
                timeLimit = SketchGuessDuration
            });
            ShowSketchGuessUI(chosenName);

            int guesserCount = Mathf.Max(0, _players.Count - 1);
            elapsed = 0f;
            while (elapsed < SketchGuessDuration && _answersThisRound.Count < guesserCount && !_skipRequested)
            {
                elapsed += Dt();
                _currentRemaining = Mathf.Max(0f, SketchGuessDuration - elapsed);
                UpdateSoloTimerUI(SketchGuessDuration);
                yield return null;
            }
            _skipRequested = false;

            _currentChosenId = null;
            _inSketchTurn = false;
            ClearSoloStage();

            if (!_players.TryGetValue(chosenId, out var chosenPlayer))
            {
                // Artist disconnected mid-turn; skip scoring and move on.
                yield break;
            }

            var deltas = new Dictionary<string, int>();
            foreach (var kv in _players) deltas[kv.Key] = 0;

            int correctCount = 0;
            foreach (var kv in _answersThisRound)
            {
                if (kv.Key == chosenId || !_players.TryGetValue(kv.Key, out var guesser)) continue;
                if (kv.Value.Choice != prompt.CorrectIndex) continue;

                correctCount++;
                guesser.Score += SketchGuesserPoints;
                deltas[kv.Key] = SketchGuesserPoints;
            }

            int artistBonus = SketchArtistPointsPerGuesser * correctCount;
            chosenPlayer.Score += artistBonus;
            deltas[chosenId] = artistBonus;

            string resultText = L.T("sketch.result", chosenName, prompt.Answer, correctCount, guesserCount, artistBonus);
            var publicList = PublicList(deltas);
            BroadcastGame(new SoloRevealPayload { title = resultText, players = publicList });
            ShowRevealUI(resultText, publicList);

            yield return new WaitForSeconds(SoloRevealDuration);
        }

        // ---- Game flow: Bible Charades ----

        private IEnumerator RunCharadesGame()
        {
            var bag = new List<string>();

            for (int i = 0; i < _charadeRounds && !_endGameRequested; i++)
            {
                string chosenId = PopNextChosenPlayer(bag);
                if (chosenId == null) break; // everyone left

                yield return RunCharadeTurn(chosenId, i, _charadeRounds);
            }

            FinishGame();
        }

        private IEnumerator RunCharadeTurn(string chosenId, int index, int total)
        {
            _currentChosenId = chosenId;
            string chosenName = _players[chosenId].Name;
            var prompt = CharadePrompts.All[Random.Range(0, CharadePrompts.All.Count)];
            var type = Random.value < 0.5f ? CharadeType.Act : CharadeType.Describe;
            string typeStr = type == CharadeType.Act ? "act" : "describe";

            _soloStage.anchoredPosition = SoloStageDefaultPos;
            _soloStage.sizeDelta = SoloStageDefaultSize;

            const float duration = CharadeDuration;
            _currentRemaining = duration;

            BroadcastGame(new CharadeTurnPayload { index = index, total = total, chosenId = chosenId, chosenName = chosenName, charadeType = typeStr, duration = duration });
            SendToPlayer(chosenId, new CharadeSecretPayload
            {
                prompt = prompt.Prompt,
                forbidden = type == CharadeType.Describe ? new List<string>(prompt.Forbidden) : new List<string>(),
                charadeType = typeStr
            });

            ShowCharadeTurnUI(chosenName, type, index, total);

            float elapsed = 0f;
            while (elapsed < duration && !_skipRequested)
            {
                elapsed += Dt();
                _currentRemaining = Mathf.Max(0f, duration - elapsed);
                UpdateSoloTimerUI(duration);
                yield return null;
            }
            _skipRequested = false;

            // ---- Guess phase: everyone but the performer picks from 4
            // options. Reuses _answersThisRound/"answer" — same shape as
            // trivia/sketch guesses.
            _answersThisRound.Clear();
            _currentRemaining = CharadeGuessDuration;

            BroadcastGame(new CharadeGuessPayload
            {
                index = index,
                total = total,
                chosenId = chosenId,
                chosenName = chosenName,
                choices = new List<string>(prompt.Choices),
                timeLimit = CharadeGuessDuration
            });
            ShowCharadeGuessUI(chosenName, type);

            int guesserCount = Mathf.Max(0, _players.Count - 1);
            elapsed = 0f;
            while (elapsed < CharadeGuessDuration && _answersThisRound.Count < guesserCount && !_skipRequested)
            {
                elapsed += Dt();
                _currentRemaining = Mathf.Max(0f, CharadeGuessDuration - elapsed);
                UpdateSoloTimerUI(CharadeGuessDuration);
                yield return null;
            }
            _skipRequested = false;

            _currentChosenId = null;

            if (!_players.TryGetValue(chosenId, out var chosenPlayer))
            {
                // Performer disconnected mid-turn; skip scoring and move on.
                yield break;
            }

            var deltas = new Dictionary<string, int>();
            foreach (var kv in _players) deltas[kv.Key] = 0;

            int correctCount = 0;
            foreach (var kv in _answersThisRound)
            {
                if (kv.Key == chosenId || !_players.TryGetValue(kv.Key, out var guesser)) continue;
                if (kv.Value.Choice != prompt.CorrectIndex) continue;

                correctCount++;
                guesser.Score += CharadeGuesserPoints;
                deltas[kv.Key] = CharadeGuesserPoints;
            }

            int bonus = CharadeArtistPointsPerGuesser * correctCount;
            chosenPlayer.Score += bonus;
            deltas[chosenId] = bonus;

            string resultText = L.T("charade.result", chosenName, prompt.Prompt, correctCount, guesserCount, bonus);
            var publicList = PublicList(deltas);
            BroadcastGame(new SoloRevealPayload { title = resultText, players = publicList });
            ShowRevealUI(resultText, publicList);

            yield return new WaitForSeconds(SoloRevealDuration);
        }

        private void TickSoloGame(SoloGameKind kind, float dt)
        {
            if (kind == SoloGameKind.FieryFurnaceDash) TickFurnaceDash(dt);
            else if (kind == SoloGameKind.DavidsSlingshot || kind == SoloGameKind.LoavesAndFishesMultiply) TickMovingTarget(dt);
        }

        private void TickFurnaceDash(float dt)
        {
            if (_soloObstacles.Count == 0)
            {
                _soloSpawnTimer -= dt;
                if (_soloSpawnTimer <= 0f) SpawnFurnaceObstacle();
                return;
            }

            var obstacle = _soloObstacles[0];
            var pos = obstacle.Rt.anchoredPosition;
            pos.y -= SoloFurnaceFallSpeed * _soloIntensity * dt;
            obstacle.Rt.anchoredPosition = pos;

            if (pos.y <= SoloPlayerY + 20f && pos.y > SoloPlayerY - 40f && obstacle.Lane == _soloLane)
            {
                Destroy(obstacle.Rt.gameObject);
                _soloObstacles.Clear();
                _soloHits++;
                FlashSoloFeedback(false);
                _soloRoundOver = true;
                _soloWon = false;
            }
            else if (pos.y < SoloPlayerY - 60f)
            {
                Destroy(obstacle.Rt.gameObject);
                _soloObstacles.Clear();
                _soloDodged++;
                if (_soloDodged >= SoloDodgeTarget)
                {
                    _soloRoundOver = true;
                    _soloWon = true;
                }
                else
                {
                    _soloSpawnTimer = SoloFurnaceSpawnGap;
                }
            }
        }

        private void SpawnFurnaceObstacle()
        {
            int lane = Random.Range(0, 3);
            var go = new GameObject("Flame", typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(_soloStage, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(70, 70);
            rt.anchoredPosition = new Vector2((lane - 1) * SoloLaneOffset, SoloSpawnY);
            go.GetComponent<Image>().color = new Color(0.85f, 0.35f, 0.15f, 0.9f);

            _soloObstacles.Add(new SoloObstacle { Rt = rt, Lane = lane });
        }

        private void TickMovingTarget(float dt)
        {
            float speed = 2.4f * _soloIntensity;
            _soloTargetTime += dt;
            _soloTargetX = SoloTargetAmplitude * Mathf.Sin(_soloTargetTime * speed);
            if (_soloTargetMarker != null)
                _soloTargetMarker.anchoredPosition = new Vector2(_soloTargetX, 100f);
        }

        private void HandleSoloMove(int direction)
        {
            if (direction == 0 || _soloRoundOver) return;
            if (_currentSoloKind != SoloGameKind.FieryFurnaceDash) return;

            _soloLane = Mathf.Clamp(_soloLane + (direction > 0 ? 1 : -1), 0, 2);
            UpdateSoloPlayerMarkerPosition();
        }

        /// <summary>Parting the Sea's control: the chosen phone tilts (or
        /// drags) like a Wii remote and continuously reports its lean, -1
        /// (full left) to 1 (full right). Scoring reuses the same "must
        /// alternate sides" idea the old discrete left/right taps used —
        /// leaning past the threshold on one side, then past it on the
        /// other, counts as one swing. The visual only hears about it once
        /// a swing actually lands (see <see cref="ISteerableSoloGameVisual.Pulse"/>),
        /// not on every raw tilt sample.</summary>
        private void HandleSoloSteer(float x)
        {
            if (_soloRoundOver || _currentSoloKind != SoloGameKind.PartingTheSea) return;

            int dir = x > SoloPartingSteerThreshold ? 1 : (x < -SoloPartingSteerThreshold ? -1 : 0);
            if (dir == 0 || dir == _soloPartingLastDir) return; // no decisive lean, or same side as last swing
            _soloPartingLastDir = dir;
            _soloPartingCount++;
            if (_soloVisual is ISteerableSoloGameVisual steerable) steerable.Pulse();
            _soloVisual?.SetProgress(_soloPartingCount / (float)SoloPartingTarget);
            if (_soloPartingCount >= SoloPartingTarget)
            {
                _soloRoundOver = true;
                _soloWon = true;
            }
        }

        private void HandleSoloFire()
        {
            if (_soloRoundOver) return;
            if (_currentSoloKind != SoloGameKind.DavidsSlingshot && _currentSoloKind != SoloGameKind.LoavesAndFishesMultiply) return;

            // One shot only — whatever happens here decides the round.
            bool hit = Mathf.Abs(_soloTargetX) <= SoloTargetHitTolerance;
            _soloRoundOver = true;
            _soloWon = hit;
            FlashSoloFeedback(hit);
        }

        private void HandleSoloShake()
        {
            if (_soloRoundOver || _currentSoloKind != SoloGameKind.JoyfulPrayer) return;
            _soloPrayerCount++;
            _soloVisual?.SetProgress(_soloPrayerCount / (float)SoloPrayerTarget);
            if (_soloPrayerCount >= SoloPrayerTarget)
            {
                _soloRoundOver = true;
                _soloWon = true;
            }
        }

        // Indices must match the PALETTE array in SketchDrawScreen.jsx.
        private static readonly Color[] SketchPalette =
        {
            new Color32(0x22, 0x12, 0x08, 0xF2), // black
            new Color32(0xC0, 0x39, 0x2B, 0xF2), // red
            new Color32(0xE0, 0x7B, 0x39, 0xF2), // orange
            new Color32(0xC9, 0xA2, 0x27, 0xF2), // gold
            new Color32(0x2E, 0x8B, 0x57, 0xF2), // green
            new Color32(0x29, 0x80, 0xB9, 0xF2), // blue
            new Color32(0x8E, 0x44, 0xAD, 0xF2), // purple
        };

        // Indices must match the BRUSH_SIZES array in SketchDrawScreen.jsx —
        // roughly 2x their phone-canvas pixel widths, since _soloStage is 2x
        // the size of the phone's drawing canvas.
        private static readonly float[] SketchBrushSizes = { 6f, 12f, 20f };

        private void HandleDrawPoint(float normalizedX, float normalizedY, bool newStroke, int colorIndex, int brushSizeIndex)
        {
            var local = new Vector2(
                (normalizedX - 0.5f) * SketchStageSize.x,
                (0.5f - normalizedY) * SketchStageSize.y);

            if (newStroke || _lastDrawPoint == null)
            {
                _lastDrawPoint = local;
                return;
            }

            Color color = SketchPalette[Mathf.Clamp(colorIndex, 0, SketchPalette.Length - 1)];
            float thickness = SketchBrushSizes[Mathf.Clamp(brushSizeIndex, 0, SketchBrushSizes.Length - 1)];
            DrawSketchSegment(_lastDrawPoint.Value, local, color, thickness);
            _lastDrawPoint = local;
        }

        private void DrawSketchSegment(Vector2 from, Vector2 to, Color color, float thickness)
        {
            var go = new GameObject("Ink", typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(_soloStage, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = (from + to) / 2f;

            float length = Mathf.Max(Vector2.Distance(from, to), 4f);
            float angle = Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg;
            rt.sizeDelta = new Vector2(length, thickness);
            rt.localRotation = Quaternion.Euler(0, 0, angle);
            go.GetComponent<Image>().color = color;

            _drawSegments.Add(rt);
        }

        private void HandleDrawClear()
        {
            foreach (var seg in _drawSegments)
            {
                if (seg != null) Destroy(seg.gameObject);
            }
            _drawSegments.Clear();
            _lastDrawPoint = null;
        }

        private string BuildSoloResultText(SoloGameKind kind, string chosenName, int points)
        {
            bool de = L.Current == Language.German;
            string outcome;
            switch (kind)
            {
                case SoloGameKind.FieryFurnaceDash:
                    outcome = _soloWon
                        ? (de ? "wich jeder Flamme aus" : "dodged every flame")
                        : (de ? "wurde von den Flammen erwischt" : "got caught in the flames");
                    break;
                case SoloGameKind.DavidsSlingshot:
                    outcome = _soloWon
                        ? (de ? "streckte Goliath mit einem Schuss nieder" : "struck Goliath down with one shot")
                        : (de ? "verfehlte den einzigen Schuss" : "missed their only shot");
                    break;
                case SoloGameKind.JoyfulPrayer:
                    outcome = _soloWon
                        ? (de ? "erfüllte den Raum mit freudigem Gebet" : "filled the room with joyful prayer")
                        : (de ? "hatte beim Beten keine Zeit mehr" : "ran out of time praying");
                    break;
                case SoloGameKind.LoavesAndFishesMultiply:
                    outcome = _soloWon
                        ? (de ? "vermehrte die Brote und Fische" : "multiplied the loaves and fishes")
                        : (de ? "vermasselte das Wunder" : "fumbled the miracle");
                    break;
                default:
                    outcome = _soloWon
                        ? (de ? "teilte das Meer" : "parted the sea")
                        : (de ? "schaffte es nicht rechtzeitig, das Wasser zu teilen" : "couldn't part the waters in time");
                    break;
            }

            if (_soloWon) return $"{chosenName} {outcome}! (+{points})";
            return de ? $"{chosenName} {outcome}. Diesmal keine Punkte." : $"{chosenName} {outcome}. No points this time.";
        }

        // ---- UI: build ----

        private void BuildUI()
        {
            var canvas = UIFactory.CreateCanvas("JesBoxCanvas");
            _canvasRoot = canvas.transform;
            UIFactory.CreateFullStretchPanel(_canvasRoot, "Background", UIFactory.BgDeep);

            _lobbyPanel = BuildLobbyPanel(_canvasRoot);
            _questionPanel = BuildQuestionPanel(_canvasRoot);
            _microgamePanel = BuildMicrogamePanel(_canvasRoot);
            _soloPanel = BuildSoloPanel(_canvasRoot);
            _revealPanel = BuildRevealPanel(_canvasRoot);
            _finalPanel = BuildFinalPanel(_canvasRoot);

            // On top of every panel — used for the quick WarioWare-style
            // screen-flash beats around Chosen One's verb intro/win-fail stamp.
            var flashRt = UIFactory.CreateFullStretchPanel(_canvasRoot, "FlashOverlay", new Color(1f, 1f, 1f, 0f));
            _flashOverlay = flashRt.GetComponent<Image>();
            _flashOverlay.raycastTarget = false;

            // Persistent host controls — sit on top of whichever mode panel is
            // currently active (added last = highest in the raycast order),
            // so pause/skip/end/kick work no matter what's on screen.
            _hostControlsBar = BuildHostControlsBar(_canvasRoot);
            _playersOverlay = BuildPlayersOverlay(_canvasRoot);
            _playersOverlay.gameObject.SetActive(false);
        }

        private RectTransform BuildHostControlsBar(Transform parent)
        {
            var group = UIFactory.CreateGroup(parent, "HostControls");

            _playersToggleButton = UIFactory.CreateButton(group, L.T("host.players"), new Vector2(830, -460), new Vector2(160, 56));
            _playersToggleButton.onClick.AddListener(TogglePlayersOverlay);

            _endGameButton = UIFactory.CreateButton(group, L.T("host.endGame"), new Vector2(650, -460), new Vector2(160, 56));
            _endGameButton.onClick.AddListener(RequestEndGame);

            _pauseButton = UIFactory.CreateButton(group, L.T("host.pause"), new Vector2(830, -524), new Vector2(160, 56));
            _pauseButton.onClick.AddListener(TogglePause);
            _pauseButtonLabel = _pauseButton.GetComponentInChildren<Text>();

            _skipButton = UIFactory.CreateButton(group, L.T("host.skip"), new Vector2(650, -524), new Vector2(160, 56));
            _skipButton.onClick.AddListener(RequestSkip);

            _pauseButton.gameObject.SetActive(false);
            _skipButton.gameObject.SetActive(false);
            _endGameButton.gameObject.SetActive(false);

            return group;
        }

        private RectTransform BuildPlayersOverlay(Transform parent)
        {
            var panel = UIFactory.CreateFullStretchPanel(parent, "PlayersOverlay", new Color(0f, 0f, 0f, 0.75f));

            UIFactory.CreateText(panel, L.T("host.playersTitle"), 36, UIFactory.Gold, TextAnchor.MiddleCenter,
                new Vector2(0, 420), new Vector2(800, 60), FontStyle.Bold);

            var closeBtn = UIFactory.CreateButton(panel, L.T("host.close"), new Vector2(0, -420), new Vector2(240, 70));
            closeBtn.onClick.AddListener(TogglePlayersOverlay);

            _playersListGroup = UIFactory.CreateGroup(panel, "PlayersList");
            _playersListGroup.anchoredPosition = new Vector2(0, 40);

            return panel;
        }

        private void RefreshPlayersOverlay()
        {
            if (_playersListGroup == null) return;
            for (int i = _playersListGroup.childCount - 1; i >= 0; i--)
                Destroy(_playersListGroup.GetChild(i).gameObject);

            const float rowHeight = 56f;
            const float rowGap = 10f;
            var players = _players.ToList();

            if (players.Count == 0)
            {
                UIFactory.CreateText(_playersListGroup, L.T("lobby.waiting"), 26, UIFactory.Cream, TextAnchor.MiddleCenter,
                    new Vector2(0, 0), new Vector2(600, 60));
                return;
            }

            float startY = (players.Count - 1) * (rowHeight + rowGap) / 2f;
            for (int i = 0; i < players.Count; i++)
            {
                var (playerId, state) = (players[i].Key, players[i].Value);
                float y = startY - i * (rowHeight + rowGap);

                string rowLabel = state.Disconnected ? L.T("lobby.playerReconnecting", state.Name) : state.Name;
                UIFactory.CreateText(_playersListGroup, $"{rowLabel} — {state.Score}", 26, UIFactory.Cream, TextAnchor.MiddleLeft,
                    new Vector2(-140, y), new Vector2(520, rowHeight));

                var kickBtn = UIFactory.CreateButton(_playersListGroup, L.T("host.kick"), new Vector2(280, y), new Vector2(140, rowHeight - 8));
                kickBtn.onClick.AddListener(() => KickPlayer(playerId));
            }
        }

        private void SelectLanguage(Language lang)
        {
            if (L.Current == lang) return;
            L.Current = lang;

            // The lobby, host controls bar, and players overlay are the only
            // pieces with static translated labels that are already built by
            // the time the host can change language, so just rebuild them in
            // place rather than tracking every Text ref. Language can only be
            // changed from the lobby screen, which means a game can't be
            // running and the players overlay can't be open — both rebuild
            // straight back to their default (hidden/closed) state safely.
            Destroy(_lobbyPanel.gameObject);
            _lobbyPanel = BuildLobbyPanel(_canvasRoot);
            ShowOnly(_lobbyPanel);
            RefreshLobbyUI();
            ApplyOrFetchQr();

            Destroy(_hostControlsBar.gameObject);
            Destroy(_playersOverlay.gameObject);
            _playersOverlayOpen = false;
            _hostControlsBar = BuildHostControlsBar(_canvasRoot);
            _playersOverlay = BuildPlayersOverlay(_canvasRoot);
            _playersOverlay.gameObject.SetActive(false);
        }

        private RectTransform BuildLobbyPanel(Transform parent)
        {
            var panel = UIFactory.CreateFullStretchPanel(parent, "LobbyPanel", Color.clear);

            UIFactory.CreateText(panel, L.T("lobby.title"), 60, UIFactory.Gold, TextAnchor.MiddleCenter,
                new Vector2(0, 500), new Vector2(1200, 80), FontStyle.Bold);
            _lobbyCodeText = UIFactory.CreateText(panel, L.T("lobby.roomCode", "----"), 44, UIFactory.Cream, TextAnchor.MiddleCenter,
                new Vector2(0, 435), new Vector2(1400, 60), FontStyle.Bold);
            UIFactory.CreateText(panel, L.T("lobby.joinAt", JoinUrlHint()), 22, UIFactory.Cream, TextAnchor.MiddleCenter,
                new Vector2(0, 390), new Vector2(1400, 36));

            UIFactory.CreateText(panel, L.T("lobby.language"), 16, UIFactory.Gold, TextAnchor.MiddleCenter,
                new Vector2(820, 530), new Vector2(200, 24));
            _languageChips = BuildChipRow(panel, new[] { "English", "Deutsch" },
                new Vector2(820, 495), 90, 46, 10, idx => SelectLanguage((Language)idx));
            HighlightChips(_languageChips, (int)L.Current);

            UIFactory.CreateText(panel, L.T("lobby.scanToJoin"), 16, UIFactory.Gold, TextAnchor.MiddleCenter,
                new Vector2(-820, 570), new Vector2(220, 24));
            var qrGo = new GameObject("QrCode", typeof(RawImage));
            _qrImage = qrGo.GetComponent<RawImage>();
            _qrImage.color = new Color(1f, 1f, 1f, 0.85f);
            var qrRt = _qrImage.rectTransform;
            qrRt.SetParent(panel, false);
            qrRt.anchorMin = new Vector2(0.5f, 0.5f);
            qrRt.anchorMax = new Vector2(0.5f, 0.5f);
            qrRt.anchoredPosition = new Vector2(-820, 360);
            qrRt.sizeDelta = new Vector2(200, 200);

            UIFactory.CreateText(panel, L.T("lobby.gameMode"), 18, UIFactory.Gold, TextAnchor.MiddleCenter,
                new Vector2(0, 335), new Vector2(400, 26));
            _modeChips = BuildChipRow(panel, new[] { L.T("mode.trivia"), L.T("mode.microgames"), L.T("mode.promptVote"), L.T("mode.chosenOne"), L.T("mode.sketch"), L.T("mode.charades") },
                new Vector2(0, 288), 190, 64, 10, idx => SelectMode((GameMode)idx));

            _triviaSettingsGroup = BuildTriviaSettingsGroup(panel, 195);
            _microgameSettingsGroup = BuildRoundsSettingsGroup(panel, 195, L.T("stepper.microgameRounds"), 2, 8, 1, _microgameRounds, v => _microgameRounds = v);
            _voteSettingsGroup = BuildRoundsSettingsGroup(panel, 195, L.T("stepper.votePrompts"), 2, VotePrompts.All.Count, 1, _votePromptCount, v => _votePromptCount = v);
            _soloSettingsGroup = BuildRoundsSettingsGroup(panel, 195, L.T("stepper.turns"), 2, 12, 1, _soloTurns, v => _soloTurns = v);
            _sketchSettingsGroup = BuildRoundsSettingsGroup(panel, 195, L.T("stepper.sketchRounds"), 2, 10, 1, _sketchRounds, v => _sketchRounds = v);
            _charadeSettingsGroup = BuildRoundsSettingsGroup(panel, 195, L.T("stepper.charadeRounds"), 2, 10, 1, _charadeRounds, v => _charadeRounds = v);

            _lobbyPlayersText = UIFactory.CreateText(panel, L.T("lobby.waiting"), 30, UIFactory.Cream, TextAnchor.MiddleCenter,
                new Vector2(0, -90), new Vector2(1200, 260));

            _startButton = UIFactory.CreateButton(panel, L.T("lobby.startGame"), new Vector2(0, -400), new Vector2(360, 90));
            _startButton.onClick.AddListener(OnStartClicked);
            _startButton.gameObject.SetActive(false);

            SelectMode(_selectedMode);
            return panel;
        }

        private RectTransform BuildTriviaSettingsGroup(Transform parent, float centerY)
        {
            var group = UIFactory.CreateGroup(parent, "TriviaSettings");

            UIFactory.CreateText(group, L.T("difficulty.label"), 18, UIFactory.Gold, TextAnchor.MiddleCenter,
                new Vector2(0, centerY + 40), new Vector2(400, 26));
            _difficultyChips = BuildChipRow(group, new[] { L.T("difficulty.easy"), L.T("difficulty.medium"), L.T("difficulty.hard") },
                new Vector2(0, centerY), 220, 56, 16, idx => SelectDifficulty((Difficulty)idx));

            UIFactory.CreateStepper(group, L.T("stepper.questions"), new Vector2(-260, centerY - 110),
                3, 10, 1, questionsPerGame, v => questionsPerGame = v);
            UIFactory.CreateStepper(group, L.T("stepper.timeLimit"), new Vector2(260, centerY - 110),
                5, 30, 5, Mathf.RoundToInt(questionTimeLimit), v => questionTimeLimit = v, "s");

            SelectDifficulty(_selectedDifficulty);
            return group;
        }

        private RectTransform BuildRoundsSettingsGroup(Transform parent, float centerY, string label,
            int min, int max, int step, int initial, System.Action<int> onChange)
        {
            var group = UIFactory.CreateGroup(parent, $"{label}Settings");
            UIFactory.CreateStepper(group, label, new Vector2(0, centerY - 40), min, max, step, initial, onChange);
            return group;
        }

        private Dictionary<int, Button> BuildChipRow(Transform parent, string[] labels, Vector2 center,
            float chipWidth, float chipHeight, float gap, System.Action<int> onSelect)
        {
            var buttons = new Dictionary<int, Button>();
            float totalWidth = labels.Length * chipWidth + (labels.Length - 1) * gap;
            float startX = center.x - totalWidth / 2f + chipWidth / 2f;

            for (int i = 0; i < labels.Length; i++)
            {
                float x = startX + i * (chipWidth + gap);
                var btn = UIFactory.CreateButton(parent, labels[i], new Vector2(x, center.y), new Vector2(chipWidth, chipHeight));
                int idx = i;
                btn.onClick.AddListener(() => onSelect(idx));
                buttons[i] = btn;
            }

            return buttons;
        }

        private static void HighlightChips(Dictionary<int, Button> chips, int selected)
        {
            if (chips == null) return;
            foreach (var kv in chips)
            {
                bool isSelected = kv.Key == selected;
                kv.Value.GetComponent<Image>().color = isSelected ? UIFactory.Gold : UIFactory.ChipUnselected;
                var txt = kv.Value.GetComponentInChildren<Text>();
                if (txt != null) txt.color = isSelected ? UIFactory.ChipTextDark : UIFactory.Cream;
            }
        }

        private void SelectMode(GameMode mode)
        {
            _selectedMode = mode;
            HighlightChips(_modeChips, (int)mode);
            _triviaSettingsGroup.gameObject.SetActive(mode == GameMode.Trivia);
            _microgameSettingsGroup.gameObject.SetActive(mode == GameMode.Microgames);
            _voteSettingsGroup.gameObject.SetActive(mode == GameMode.PromptVote);
            _soloSettingsGroup.gameObject.SetActive(mode == GameMode.ChosenOne);
            _sketchSettingsGroup.gameObject.SetActive(mode == GameMode.Sketch);
            _charadeSettingsGroup.gameObject.SetActive(mode == GameMode.Charades);
        }

        private void SelectDifficulty(Difficulty difficulty)
        {
            _selectedDifficulty = difficulty;
            HighlightChips(_difficultyChips, (int)difficulty);
        }

        private (Text text, Image fill) BuildTimerWidget(Transform parent, float y)
        {
            var timerText = UIFactory.CreateText(parent, "", 70, UIFactory.Gold, TextAnchor.MiddleCenter,
                new Vector2(0, y), new Vector2(200, 100), FontStyle.Bold);

            var barBgGo = new GameObject("TimerBarBg", typeof(Image));
            var barBgRt = barBgGo.GetComponent<RectTransform>();
            barBgRt.SetParent(parent, false);
            barBgRt.anchorMin = new Vector2(0.5f, 0.5f);
            barBgRt.anchorMax = new Vector2(0.5f, 0.5f);
            barBgRt.anchoredPosition = new Vector2(0, y - 100);
            barBgRt.sizeDelta = new Vector2(800, 24);
            barBgGo.GetComponent<Image>().color = new Color(1, 1, 1, 0.15f);

            var barFillGo = new GameObject("TimerBarFill", typeof(Image));
            var barFillRt = barFillGo.GetComponent<RectTransform>();
            barFillRt.SetParent(barBgRt, false);
            barFillRt.anchorMin = Vector2.zero;
            barFillRt.anchorMax = Vector2.one;
            barFillRt.offsetMin = Vector2.zero;
            barFillRt.offsetMax = Vector2.zero;
            var fillImg = barFillGo.GetComponent<Image>();
            fillImg.color = UIFactory.Gold;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 1f;

            return (timerText, fillImg);
        }

        private RectTransform BuildQuestionPanel(Transform parent)
        {
            var panel = UIFactory.CreateFullStretchPanel(parent, "QuestionPanel", Color.clear);
            _questionHeaderText = UIFactory.CreateText(panel, "", 36, UIFactory.Gold, TextAnchor.MiddleCenter,
                new Vector2(0, 420), new Vector2(1000, 60));
            _questionBodyText = UIFactory.CreateText(panel, "", 52, UIFactory.Cream, TextAnchor.MiddleCenter,
                new Vector2(0, 260), new Vector2(1500, 200), FontStyle.Bold);
            _questionChoicesText = UIFactory.CreateText(panel, "", 38, UIFactory.Cream, TextAnchor.MiddleCenter,
                new Vector2(0, 20), new Vector2(1200, 320));

            var (timerText, fill) = BuildTimerWidget(panel, -320);
            _questionTimerText = timerText;
            _questionTimerFill = fill;

            return panel;
        }

        private RectTransform BuildMicrogamePanel(Transform parent)
        {
            var panel = UIFactory.CreateFullStretchPanel(parent, "MicrogamePanel", Color.clear);
            _microgameHeaderText = UIFactory.CreateText(panel, "", 36, UIFactory.Gold, TextAnchor.MiddleCenter,
                new Vector2(0, 420), new Vector2(1000, 60));
            _microgameTitleText = UIFactory.CreateText(panel, "", 64, UIFactory.Cream, TextAnchor.MiddleCenter,
                new Vector2(0, 260), new Vector2(1500, 120), FontStyle.Bold);
            _microgameInstructionsText = UIFactory.CreateText(panel, "", 34, UIFactory.Cream, TextAnchor.MiddleCenter,
                new Vector2(0, 100), new Vector2(1300, 160));

            var (timerText, fill) = BuildTimerWidget(panel, -280);
            _microgameTimerText = timerText;
            _microgameTimerFill = fill;

            return panel;
        }

        private RectTransform BuildSoloPanel(Transform parent)
        {
            var panel = UIFactory.CreateFullStretchPanel(parent, "SoloPanel", Color.clear);

            // The "X is up! <Game Title>" prompt — shown only briefly during
            // PlaySoloIntro (see SetSoloPromptVisible), then hidden so the
            // enlarged stage below has the whole screen to itself, WarioWare-style.
            _soloHeaderText = UIFactory.CreateText(panel, "", 32, UIFactory.Gold, TextAnchor.MiddleCenter,
                new Vector2(0, 460), new Vector2(1000, 50));
            _soloChosenNameText = UIFactory.CreateText(panel, "", 46, UIFactory.Gold, TextAnchor.MiddleCenter,
                new Vector2(0, 395), new Vector2(1400, 60), FontStyle.Bold);
            _soloTitleText = UIFactory.CreateText(panel, "", 42, UIFactory.Cream, TextAnchor.MiddleCenter,
                new Vector2(0, 330), new Vector2(1400, 60), FontStyle.Bold);
            _soloInstructionsText = UIFactory.CreateText(panel, "", 24, UIFactory.Cream, TextAnchor.MiddleCenter,
                new Vector2(0, 270), new Vector2(1200, 60));

            var stageGo = new GameObject("SoloStage", typeof(Image));
            _soloStage = stageGo.GetComponent<RectTransform>();
            _soloStage.SetParent(panel, false);
            _soloStage.anchorMin = new Vector2(0.5f, 0.5f);
            _soloStage.anchorMax = new Vector2(0.5f, 0.5f);
            _soloStage.anchoredPosition = SoloStageDefaultPos;
            _soloStage.sizeDelta = SoloStageDefaultSize;
            stageGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.05f);

            // Sits right at the bottom edge of the screen, below the stage.
            var (timerText, fill) = BuildTimerWidget(panel, -425);
            _soloTimerText = timerText;
            _soloTimerFill = fill;

            // WarioWare-style verb flash ("DODGE!", "FIRE!", ...) and win/fail
            // stamp ("✓"/"✗") — created after the stage so it renders on top,
            // hidden until PlaySoloIntro/PlaySoloStamp activates it.
            _soloVerbText = UIFactory.CreateText(panel, "", 120, UIFactory.Gold, TextAnchor.MiddleCenter,
                SoloStageDefaultPos, new Vector2(1200, 300), FontStyle.Bold);
            _soloVerbText.gameObject.SetActive(false);

            return panel;
        }

        /// <summary>Shows/hides the "Turn X/Y" / "Name is up!" / title / cheer
        /// prompt — visible only for the brief intro beat before a Chosen One
        /// round starts, then hidden so the (now much bigger) stage has the
        /// whole screen to itself while the round is actually playing.</summary>
        private void SetSoloPromptVisible(bool visible)
        {
            _soloHeaderText.gameObject.SetActive(visible);
            _soloChosenNameText.gameObject.SetActive(visible);
            _soloTitleText.gameObject.SetActive(visible);
            _soloInstructionsText.gameObject.SetActive(visible);
        }

        private RectTransform BuildRevealPanel(Transform parent)
        {
            var panel = UIFactory.CreateFullStretchPanel(parent, "RevealPanel", Color.clear);
            _revealBannerText = UIFactory.CreateText(panel, "", 46, UIFactory.Gold, TextAnchor.MiddleCenter,
                new Vector2(0, 300), new Vector2(1500, 140), FontStyle.Bold);
            _revealScoresText = UIFactory.CreateText(panel, "", 38, UIFactory.Cream, TextAnchor.MiddleCenter,
                new Vector2(0, 0), new Vector2(1000, 500));
            return panel;
        }

        private RectTransform BuildFinalPanel(Transform parent)
        {
            var panel = UIFactory.CreateFullStretchPanel(parent, "FinalPanel", Color.clear);
            UIFactory.CreateText(panel, L.T("final.title"), 70, UIFactory.Gold, TextAnchor.MiddleCenter,
                new Vector2(0, 380), new Vector2(1200, 100), FontStyle.Bold);
            _finalScoresText = UIFactory.CreateText(panel, "", 40, UIFactory.Cream, TextAnchor.MiddleCenter,
                new Vector2(0, 0), new Vector2(1000, 600));
            _backToMenuButton = UIFactory.CreateButton(panel, L.T("final.backToMenu"), new Vector2(0, -420), new Vector2(420, 90));
            _backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
            return panel;
        }

        // ---- UI: update ----

        private void ShowOnly(RectTransform panel)
        {
            _lobbyPanel.gameObject.SetActive(panel == _lobbyPanel);
            _questionPanel.gameObject.SetActive(panel == _questionPanel);
            _microgamePanel.gameObject.SetActive(panel == _microgamePanel);
            _soloPanel.gameObject.SetActive(panel == _soloPanel);
            _revealPanel.gameObject.SetActive(panel == _revealPanel);
            _finalPanel.gameObject.SetActive(panel == _finalPanel);
        }

        private void RefreshLobbyUI()
        {
            _lobbyCodeText.text = L.T("lobby.roomCode", _roomCode);
            _lobbyPlayersText.text = _players.Count == 0
                ? L.T("lobby.waiting")
                : string.Join("\n", _players.Values.Select(p => p.Disconnected ? L.T("lobby.playerReconnecting", p.Name) : p.Name));
            _startButton.gameObject.SetActive(!_gameRunning && _players.Count > 0);
        }

        private void ShowQuestionUI(TriviaQuestion q, int index, int total)
        {
            ShowOnly(_questionPanel);
            _sound.PlayTick();
            _questionHeaderText.text = L.T("question.header", index + 1, total);
            _questionBodyText.text = q.Question;
            string[] letters = { "A", "B", "C", "D" };
            _questionChoicesText.text = string.Join("\n", q.Choices.Select((c, i) => $"{letters[i]}) {c}"));
        }

        private void ShowVotePromptUI(VotePrompt prompt, int index, int total)
        {
            ShowOnly(_questionPanel);
            _sound.PlayTick();
            _questionHeaderText.text = L.T("vote.header", index + 1, total);
            _questionBodyText.text = prompt.Scenario;
            string[] letters = { "A", "B", "C", "D" };
            _questionChoicesText.text = string.Join("\n", prompt.Options.Select((o, i) => $"{letters[i]}) {o}"));
        }

        private void UpdateQuestionTimerUI()
        {
            _questionTimerText.text = Mathf.CeilToInt(_currentRemaining).ToString();
            float total = _selectedMode == GameMode.Trivia ? questionTimeLimit : 12f;
            if (_questionTimerFill != null)
                _questionTimerFill.fillAmount = total <= 0 ? 0 : _currentRemaining / total;
            MaybeBeepCountdown();
        }

        private void ShowMicrogameUI(MicrogameDef def, int index, int total)
        {
            ShowOnly(_microgamePanel);
            _sound.PlayTick();
            _microgameHeaderText.text = L.T("microgame.header", index + 1, total);
            _microgameTitleText.text = def.Title;
            _microgameInstructionsText.text = def.Instructions;
        }

        private void UpdateMicrogameTimerUI(float duration)
        {
            _microgameTimerText.text = Mathf.CeilToInt(_currentRemaining).ToString();
            if (_microgameTimerFill != null)
                _microgameTimerFill.fillAmount = duration <= 0 ? 0 : _currentRemaining / duration;
        }

        private void ShowSoloTurnUI(SoloGameDef def, string chosenName, int index, int total)
        {
            ShowOnly(_soloPanel);
            SetSoloPromptVisible(true);
            _sound.PlayTick();
            _soloHeaderText.text = L.T("solo.header", index + 1, total);
            _soloChosenNameText.text = L.T("solo.isUp", chosenName);
            _soloTitleText.text = def.Title;
            _soloInstructionsText.text = L.T("solo.cheer");
        }

        private void UpdateSoloTimerUI(float duration)
        {
            _soloTimerText.text = Mathf.CeilToInt(_currentRemaining).ToString();
            if (_soloTimerFill != null)
                _soloTimerFill.fillAmount = duration <= 0 ? 0 : _currentRemaining / duration;
            MaybeBeepCountdown();
        }

        private void MaybeBeepCountdown()
        {
            int sec = Mathf.CeilToInt(_currentRemaining);
            if (sec <= 3 && sec > 0 && sec != _lastCountdownBeepSecond)
            {
                _lastCountdownBeepSecond = sec;
                _sound.PlayCountdownBeep();
            }
        }

        private void ShowSketchDrawUI(string chosenName, int index, int total)
        {
            ShowOnly(_soloPanel);
            SetSoloPromptVisible(true); // in case a prior Chosen One session left these hidden
            _sound.PlayTick();
            _soloHeaderText.text = L.T("sketch.header", index + 1, total);
            _soloChosenNameText.text = L.T("sketch.isDrawing", chosenName);
            _soloTitleText.text = L.T("sketch.title");
            _soloInstructionsText.text = L.T("sketch.watchHint");
        }

        private void ShowSketchGuessUI(string chosenName)
        {
            // Keep _soloPanel/_soloStage as-is so the finished drawing stays
            // visible while everyone guesses — just update the text above it.
            _soloChosenNameText.text = L.T("sketch.whatDidDraw", chosenName);
            _soloInstructionsText.text = L.T("sketch.guessOnPhone");
        }

        private void ShowCharadeTurnUI(string chosenName, CharadeType type, int index, int total)
        {
            ShowOnly(_soloPanel);
            SetSoloPromptVisible(true); // in case a prior Chosen One session left these hidden
            _sound.PlayTick();
            _soloHeaderText.text = L.T("charade.header", index + 1, total);
            _soloChosenNameText.text = L.T("solo.isUp", chosenName);
            _soloTitleText.text = L.T("mode.charades");
            _soloInstructionsText.text = type == CharadeType.Act ? L.T("charade.actInstructions") : L.T("charade.describeInstructions");
        }

        private void ShowCharadeGuessUI(string chosenName, CharadeType type)
        {
            _soloChosenNameText.text = type == CharadeType.Act
                ? L.T("charade.whatActed", chosenName)
                : L.T("charade.whatDescribed", chosenName);
            _soloInstructionsText.text = L.T("sketch.guessOnPhone");
        }

        private void SetupSoloStage(SoloGameKind kind)
        {
            ClearSoloStage();

            if (kind == SoloGameKind.FieryFurnaceDash)
            {
                for (int lane = -1; lane <= 1; lane++)
                {
                    if (lane == 0) continue; // no divider needed through the middle lane's own center
                    var lineGo = new GameObject("LaneDivider", typeof(Image));
                    var lineRt = lineGo.GetComponent<RectTransform>();
                    lineRt.SetParent(_soloStage, false);
                    lineRt.anchorMin = new Vector2(0.5f, 0.5f);
                    lineRt.anchorMax = new Vector2(0.5f, 0.5f);
                    lineRt.anchoredPosition = new Vector2(lane * SoloLaneOffset * 0.5f, 0);
                    lineRt.sizeDelta = new Vector2(4, 400);
                    lineGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
                }

                var go = new GameObject("Player", typeof(Image));
                _soloPlayerMarker = go.GetComponent<RectTransform>();
                _soloPlayerMarker.SetParent(_soloStage, false);
                _soloPlayerMarker.anchorMin = new Vector2(0.5f, 0.5f);
                _soloPlayerMarker.anchorMax = new Vector2(0.5f, 0.5f);
                _soloPlayerMarker.sizeDelta = new Vector2(80, 80);
                go.GetComponent<Image>().color = UIFactory.Gold;
                UpdateSoloPlayerMarkerPosition();
            }
            else if (kind == SoloGameKind.DavidsSlingshot || kind == SoloGameKind.LoavesAndFishesMultiply)
            {
                bool isSlingshot = kind == SoloGameKind.DavidsSlingshot;

                var zoneGo = new GameObject("TargetZone", typeof(Image));
                var zoneRt = zoneGo.GetComponent<RectTransform>();
                zoneRt.SetParent(_soloStage, false);
                zoneRt.anchorMin = new Vector2(0.5f, 0.5f);
                zoneRt.anchorMax = new Vector2(0.5f, 0.5f);
                zoneRt.anchoredPosition = new Vector2(0, 100);
                zoneRt.sizeDelta = new Vector2(SoloTargetHitTolerance * 2, 140);
                zoneGo.GetComponent<Image>().color = new Color(0.9f, 0.85f, 0.4f, 0.25f);

                var markerGo = new GameObject(isSlingshot ? "Goliath" : "Basket", typeof(Image));
                _soloTargetMarker = markerGo.GetComponent<RectTransform>();
                _soloTargetMarker.SetParent(_soloStage, false);
                _soloTargetMarker.anchorMin = new Vector2(0.5f, 0.5f);
                _soloTargetMarker.anchorMax = new Vector2(0.5f, 0.5f);
                _soloTargetMarker.anchoredPosition = new Vector2(0, 100);
                _soloTargetMarker.sizeDelta = new Vector2(90, 90);
                markerGo.GetComponent<Image>().color = isSlingshot
                    ? new Color(0.55f, 0.2f, 0.2f, 0.95f)
                    : new Color(0.85f, 0.65f, 0.25f, 0.95f);
            }
            else if (kind == SoloGameKind.JoyfulPrayer || kind == SoloGameKind.PartingTheSea)
            {
                _soloVisual = SoloGameVisualFactory.Create(kind);
                _soloVisual?.Setup(_soloStage);
            }
        }

        private void UpdateSoloPlayerMarkerPosition()
        {
            if (_soloPlayerMarker == null) return;
            _soloPlayerMarker.anchoredPosition = new Vector2((_soloLane - 1) * SoloLaneOffset, SoloPlayerY);
        }

        private void ClearSoloStage()
        {
            // Tear down before the blanket child-destroy below so a visual's
            // Teardown() (which may release a RenderTexture or destroy a
            // camera living outside the stage hierarchy) runs against still-
            // valid objects rather than ones the loop already swept.
            _soloVisual?.Teardown();
            _soloVisual = null;

            foreach (var obstacle in _soloObstacles)
            {
                if (obstacle.Rt != null) Destroy(obstacle.Rt.gameObject);
            }
            _soloObstacles.Clear();
            _soloPlayerMarker = null;
            _soloTargetMarker = null;
            _drawSegments.Clear();
            _lastDrawPoint = null;

            if (_soloStage == null) return;
            for (int i = _soloStage.childCount - 1; i >= 0; i--)
            {
                Destroy(_soloStage.GetChild(i).gameObject);
            }
        }

        private void FlashSoloFeedback(bool success)
        {
            RectTransform markerRt = _currentSoloKind == SoloGameKind.FieryFurnaceDash ? _soloPlayerMarker : _soloTargetMarker;
            Image target = markerRt != null ? markerRt.GetComponent<Image>() : null;
            if (target != null) StartCoroutine(FlashRoutine(target, success ? UIFactory.Gold : new Color(0.85f, 0.2f, 0.2f)));
        }

        private IEnumerator FlashRoutine(Image target, Color flashColor)
        {
            Color original = target.color;
            target.color = flashColor;
            yield return new WaitForSeconds(0.15f);
            if (target != null) target.color = original;
        }

        // ---- WarioWare-style beats: verb intro + win/fail stamp ----

        /// <summary>Quick punch/pop scale animation: from -> peak -> to.</summary>
        private IEnumerator PunchScale(RectTransform rt, float from, float peak, float to, float duration)
        {
            float upTime = duration * 0.4f;
            float downTime = duration - upTime;
            rt.localScale = Vector3.one * from;

            float t = 0f;
            while (t < upTime)
            {
                t += Time.deltaTime;
                rt.localScale = Vector3.one * Mathf.Lerp(from, peak, Mathf.Clamp01(t / upTime));
                yield return null;
            }

            t = 0f;
            while (t < downTime)
            {
                t += Time.deltaTime;
                rt.localScale = Vector3.one * Mathf.Lerp(peak, to, Mathf.Clamp01(t / downTime));
                yield return null;
            }
            rt.localScale = Vector3.one * to;
        }

        /// <summary>Brief full-screen color flash — a quick fade up then back to clear.</summary>
        private IEnumerator FlashScreenRoutine(Color color, float peakAlpha, float duration)
        {
            if (_flashOverlay == null) yield break;
            float half = duration * 0.5f;

            float t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                _flashOverlay.color = new Color(color.r, color.g, color.b, Mathf.Lerp(0f, peakAlpha, t / half));
                yield return null;
            }
            t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                _flashOverlay.color = new Color(color.r, color.g, color.b, Mathf.Lerp(peakAlpha, 0f, t / half));
                yield return null;
            }
            _flashOverlay.color = new Color(color.r, color.g, color.b, 0f);
        }

        /// <summary>Flashes the round's command ("DODGE!", "FIRE!", ...) big and
        /// bold before gameplay actually starts — the stage is already set up
        /// but not yet ticking, so nothing moves during this beat.</summary>
        private IEnumerator PlaySoloIntro(SoloGameDef def)
        {
            _soloVerbText.text = def.Verb;
            _soloVerbText.color = UIFactory.Gold;
            _soloVerbText.gameObject.SetActive(true);
            _sound.PlayGo();
            StartCoroutine(FlashScreenRoutine(Color.white, 0.35f, 0.25f));
            StartCoroutine(PunchScale(_soloStage, 0.85f, 1.05f, 1f, 0.35f));

            yield return PunchScale(_soloVerbText.rectTransform, 0.4f, 1.25f, 1f, SoloIntroDuration);
            yield return new WaitForSeconds(0.15f);
            _soloVerbText.gameObject.SetActive(false);
        }

        /// <summary>Big instant win/fail judgment stamp shown right after the
        /// round ends, before cutting to the scoreboard reveal.</summary>
        private IEnumerator PlaySoloStamp(bool won)
        {
            _soloVerbText.text = won ? "✓" : "✗";
            _soloVerbText.color = won ? UIFactory.Gold : new Color(0.85f, 0.25f, 0.2f);
            _soloVerbText.gameObject.SetActive(true);
            if (won) _sound.PlaySuccessStamp(); else _sound.PlayFailStamp();
            StartCoroutine(FlashScreenRoutine(won ? UIFactory.Gold : new Color(0.85f, 0.2f, 0.2f), 0.3f, 0.3f));

            yield return PunchScale(_soloVerbText.rectTransform, 0.3f, 1.3f, 1f, SoloStampDuration);
            yield return new WaitForSeconds(0.35f);
            _soloVerbText.gameObject.SetActive(false);
        }

        private void ShowRevealUI(string bannerText, List<PlayerPublic> players)
        {
            ShowOnly(_revealPanel);
            _sound.PlayReveal();
            _revealBannerText.text = bannerText;
            var top = players.OrderByDescending(p => p.score).Take(5);
            _revealScoresText.text = string.Join("\n", top.Select((p, i) => $"{i + 1}. {p.name} — {p.score} ({(p.delta > 0 ? "+" + p.delta : "0")})"));
        }

        private void ShowFinalUI(List<PlayerPublic> sorted)
        {
            ShowOnly(_finalPanel);
            _sound.PlayVictoryFanfare();
            _finalScoresText.text = string.Join("\n", sorted.Select((p, i) => $"{i + 1}. {p.name} — {p.score}"));
        }
    }
}
