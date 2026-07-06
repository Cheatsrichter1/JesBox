using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JesBox.Net;
using JesBox.UI;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
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

        private enum GameMode { Trivia, Microgames, PromptVote }

        private class PlayerState
        {
            public string Name;
            public int Score;
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

        // Host-selected settings
        private GameMode _selectedMode = GameMode.Trivia;
        private Difficulty _selectedDifficulty = Difficulty.Medium;
        private int _microgameRounds = 4;
        private int _votePromptCount = 5;

        // UI references
        private RectTransform _lobbyPanel, _questionPanel, _microgamePanel, _revealPanel, _finalPanel;
        private RectTransform _triviaSettingsGroup, _microgameSettingsGroup, _voteSettingsGroup;
        private Text _lobbyCodeText, _lobbyPlayersText;
        private Button _startButton;
        private Dictionary<int, Button> _modeChips;
        private Dictionary<int, Button> _difficultyChips;
        private Text _questionHeaderText, _questionBodyText, _questionChoicesText, _questionTimerText;
        private Image _questionTimerFill;
        private Text _microgameHeaderText, _microgameTitleText, _microgameInstructionsText, _microgameTimerText;
        private Image _microgameTimerFill;
        private Text _revealBannerText, _revealScoresText;
        private Text _finalScoresText;
        private Button _backToMenuButton;

        private void Awake()
        {
            _net = gameObject.AddComponent<NetworkClient>();
            _net.OnOpen += HandleOpen;
            _net.OnTextMessage += HandleMessage;
            _net.OnError += err => Debug.LogWarning($"[JesBox] Socket error: {err}");
            _net.OnClose += () => Debug.LogWarning("[JesBox] Socket closed.");

            EnsureEventSystem();
            BuildUI();
            ShowOnly(_lobbyPanel);
        }

        private void Start()
        {
            _net.Connect(serverUrl);
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
                    break;

                case "player_joined":
                {
                    var joined = JsonConvert.DeserializeObject<PlayerJoinedIn>(raw);
                    _players[joined.playerId] = new PlayerState { Name = joined.name, Score = 0 };
                    RefreshLobbyUI();
                    BroadcastLobby();
                    break;
                }

                case "player_left":
                {
                    var left = JsonConvert.DeserializeObject<PlayerLeftIn>(raw);
                    _players.Remove(left.playerId);
                    RefreshLobbyUI();
                    BroadcastLobby();
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
            _net.SendJson(new GameOut<LobbyPayload> { data = new LobbyPayload { players = PublicList() } });
        }

        // ---- Game flow: shared ----

        private void OnStartClicked()
        {
            if (_gameRunning || _players.Count == 0) return;
            _gameRunning = true;
            _startButton.gameObject.SetActive(false);

            switch (_selectedMode)
            {
                case GameMode.Trivia: StartCoroutine(RunTriviaGame()); break;
                case GameMode.Microgames: StartCoroutine(RunMicrogames()); break;
                case GameMode.PromptVote: StartCoroutine(RunPromptVote()); break;
            }
        }

        private void OnBackToMenuClicked()
        {
            foreach (var player in _players.Values) player.Score = 0;
            _gameRunning = false;
            _answersThisRound.Clear();
            _tapCounts.Clear();
            _submittedScores.Clear();
            _votesThisRound.Clear();
            RefreshLobbyUI();
            BroadcastLobby();
            ShowOnly(_lobbyPanel);
        }

        private void FinishGame()
        {
            var sorted = PublicList().OrderByDescending(p => p.score).ToList();
            _net.SendJson(new GameOut<FinalPayload> { data = new FinalPayload { players = sorted } });
            ShowFinalUI(sorted);
            _gameRunning = false;
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

            for (int i = 0; i < count; i++)
            {
                yield return RunQuestion(pool[i], i, count);
            }

            FinishGame();
        }

        private IEnumerator RunQuestion(TriviaQuestion q, int index, int total)
        {
            _answersThisRound.Clear();
            _currentRemaining = questionTimeLimit;

            _net.SendJson(new GameOut<QuestionPayload>
            {
                data = new QuestionPayload
                {
                    index = index,
                    total = total,
                    question = q.Question,
                    choices = new List<string>(q.Choices),
                    timeLimit = questionTimeLimit
                }
            });
            ShowQuestionUI(q, index, total);

            float elapsed = 0f;
            while (elapsed < questionTimeLimit && _answersThisRound.Count < _players.Count)
            {
                elapsed += Time.deltaTime;
                _currentRemaining = Mathf.Max(0f, questionTimeLimit - elapsed);
                UpdateQuestionTimerUI();
                yield return null;
            }

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
            _net.SendJson(new GameOut<RevealPayload> { data = new RevealPayload { correctIndex = q.CorrectIndex, players = publicList } });
            string[] letters = { "A", "B", "C", "D" };
            ShowRevealUI($"Correct answer: {letters[q.CorrectIndex]}) {q.Choices[q.CorrectIndex]}", publicList);

            yield return new WaitForSeconds(revealDuration);
        }

        // ---- Game flow: Microgames ----

        private IEnumerator RunMicrogames()
        {
            var defs = new List<MicrogameDef>(Microgames.All);
            Shuffle(defs);

            for (int i = 0; i < _microgameRounds; i++)
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

            _net.SendJson(new GameOut<MicrogamePayload>
            {
                data = new MicrogamePayload
                {
                    index = index,
                    total = total,
                    kind = def.Kind.ToString(),
                    title = def.Title,
                    instructions = def.Instructions,
                    duration = def.Duration
                }
            });
            ShowMicrogameUI(def, index, total);

            float elapsed = 0f;
            while (elapsed < def.Duration)
            {
                elapsed += Time.deltaTime;
                _currentRemaining = Mathf.Max(0f, def.Duration - elapsed);
                UpdateMicrogameTimerUI(def.Duration);
                yield return null;
            }

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
            _net.SendJson(new GameOut<MicrogameRevealPayload> { data = new MicrogameRevealPayload { title = $"{def.Title} — Results!", players = publicList } });
            ShowRevealUI($"{def.Title} — Results!", publicList);

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

            for (int i = 0; i < count; i++)
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

            _net.SendJson(new GameOut<VotePromptPayload>
            {
                data = new VotePromptPayload
                {
                    index = index,
                    total = total,
                    scenario = prompt.Scenario,
                    options = new List<string>(prompt.Options),
                    timeLimit = duration
                }
            });
            ShowVotePromptUI(prompt, index, total);

            float elapsed = 0f;
            while (elapsed < duration && _votesThisRound.Count < _players.Count)
            {
                elapsed += Time.deltaTime;
                _currentRemaining = Mathf.Max(0f, duration - elapsed);
                UpdateQuestionTimerUI();
                yield return null;
            }

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
            _net.SendJson(new GameOut<VoteRevealPayload>
            {
                data = new VoteRevealPayload { tally = new List<int>(tally), favoriteIndex = favoriteIndex, players = publicList }
            });

            string[] letters = { "A", "B", "C", "D" };
            string banner = favoriteIndex >= 0
                ? $"Crowd favorite: {letters[favoriteIndex]}) {prompt.Options[favoriteIndex]}"
                : "No votes cast!";
            ShowRevealUI(banner, publicList);

            yield return new WaitForSeconds(revealDuration);
        }

        // ---- UI: build ----

        private void BuildUI()
        {
            var canvas = UIFactory.CreateCanvas("JesBoxCanvas");
            UIFactory.CreateFullStretchPanel(canvas.transform, "Background", UIFactory.BgDeep);

            _lobbyPanel = BuildLobbyPanel(canvas.transform);
            _questionPanel = BuildQuestionPanel(canvas.transform);
            _microgamePanel = BuildMicrogamePanel(canvas.transform);
            _revealPanel = BuildRevealPanel(canvas.transform);
            _finalPanel = BuildFinalPanel(canvas.transform);
        }

        private RectTransform BuildLobbyPanel(Transform parent)
        {
            var panel = UIFactory.CreateFullStretchPanel(parent, "LobbyPanel", Color.clear);

            UIFactory.CreateText(panel, "JESBOX", 60, UIFactory.Gold, TextAnchor.MiddleCenter,
                new Vector2(0, 500), new Vector2(1200, 80), FontStyle.Bold);
            _lobbyCodeText = UIFactory.CreateText(panel, "ROOM CODE: ----", 44, UIFactory.Cream, TextAnchor.MiddleCenter,
                new Vector2(0, 435), new Vector2(1400, 60), FontStyle.Bold);
            UIFactory.CreateText(panel, $"Join at {JoinUrlHint()}", 22, UIFactory.Cream, TextAnchor.MiddleCenter,
                new Vector2(0, 390), new Vector2(1400, 36));

            UIFactory.CreateText(panel, "GAME MODE", 18, UIFactory.Gold, TextAnchor.MiddleCenter,
                new Vector2(0, 335), new Vector2(400, 26));
            _modeChips = BuildChipRow(panel, new[] { "Trivia Quiz", "Microgames", "Prompt & Vote" },
                new Vector2(0, 288), 340, 64, 20, idx => SelectMode((GameMode)idx));

            _triviaSettingsGroup = BuildTriviaSettingsGroup(panel, 195);
            _microgameSettingsGroup = BuildRoundsSettingsGroup(panel, 195, "Microgame Rounds", 2, 8, 1, _microgameRounds, v => _microgameRounds = v);
            _voteSettingsGroup = BuildRoundsSettingsGroup(panel, 195, "Vote Prompts", 2, VotePrompts.All.Count, 1, _votePromptCount, v => _votePromptCount = v);

            _lobbyPlayersText = UIFactory.CreateText(panel, "Waiting for players...", 30, UIFactory.Cream, TextAnchor.MiddleCenter,
                new Vector2(0, -90), new Vector2(1200, 260));

            _startButton = UIFactory.CreateButton(panel, "Start Game", new Vector2(0, -400), new Vector2(360, 90));
            _startButton.onClick.AddListener(OnStartClicked);
            _startButton.gameObject.SetActive(false);

            SelectMode(GameMode.Trivia);
            return panel;
        }

        private RectTransform BuildTriviaSettingsGroup(Transform parent, float centerY)
        {
            var group = UIFactory.CreateGroup(parent, "TriviaSettings");

            UIFactory.CreateText(group, "DIFFICULTY", 18, UIFactory.Gold, TextAnchor.MiddleCenter,
                new Vector2(0, centerY + 40), new Vector2(400, 26));
            _difficultyChips = BuildChipRow(group, new[] { "Easy", "Medium", "Hard" },
                new Vector2(0, centerY), 220, 56, 16, idx => SelectDifficulty((Difficulty)idx));

            UIFactory.CreateStepper(group, "Questions", new Vector2(-260, centerY - 110),
                3, 10, 1, questionsPerGame, v => questionsPerGame = v);
            UIFactory.CreateStepper(group, "Time Limit", new Vector2(260, centerY - 110),
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
            UIFactory.CreateText(panel, "FINAL SCORES", 70, UIFactory.Gold, TextAnchor.MiddleCenter,
                new Vector2(0, 380), new Vector2(1200, 100), FontStyle.Bold);
            _finalScoresText = UIFactory.CreateText(panel, "", 40, UIFactory.Cream, TextAnchor.MiddleCenter,
                new Vector2(0, 0), new Vector2(1000, 600));
            _backToMenuButton = UIFactory.CreateButton(panel, "Back to Main Menu", new Vector2(0, -420), new Vector2(420, 90));
            _backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
            return panel;
        }

        // ---- UI: update ----

        private void ShowOnly(RectTransform panel)
        {
            _lobbyPanel.gameObject.SetActive(panel == _lobbyPanel);
            _questionPanel.gameObject.SetActive(panel == _questionPanel);
            _microgamePanel.gameObject.SetActive(panel == _microgamePanel);
            _revealPanel.gameObject.SetActive(panel == _revealPanel);
            _finalPanel.gameObject.SetActive(panel == _finalPanel);
        }

        private void RefreshLobbyUI()
        {
            _lobbyCodeText.text = $"ROOM CODE: {_roomCode}";
            _lobbyPlayersText.text = _players.Count == 0
                ? "Waiting for players..."
                : string.Join("\n", _players.Values.Select(p => p.Name));
            _startButton.gameObject.SetActive(!_gameRunning && _players.Count > 0);
        }

        private void ShowQuestionUI(TriviaQuestion q, int index, int total)
        {
            ShowOnly(_questionPanel);
            _questionHeaderText.text = $"Question {index + 1} / {total}";
            _questionBodyText.text = q.Question;
            string[] letters = { "A", "B", "C", "D" };
            _questionChoicesText.text = string.Join("\n", q.Choices.Select((c, i) => $"{letters[i]}) {c}"));
        }

        private void ShowVotePromptUI(VotePrompt prompt, int index, int total)
        {
            ShowOnly(_questionPanel);
            _questionHeaderText.text = $"Vote! Prompt {index + 1} / {total}";
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
        }

        private void ShowMicrogameUI(MicrogameDef def, int index, int total)
        {
            ShowOnly(_microgamePanel);
            _microgameHeaderText.text = $"Microgame {index + 1} / {total}";
            _microgameTitleText.text = def.Title;
            _microgameInstructionsText.text = def.Instructions;
        }

        private void UpdateMicrogameTimerUI(float duration)
        {
            _microgameTimerText.text = Mathf.CeilToInt(_currentRemaining).ToString();
            if (_microgameTimerFill != null)
                _microgameTimerFill.fillAmount = duration <= 0 ? 0 : _currentRemaining / duration;
        }

        private void ShowRevealUI(string bannerText, List<PlayerPublic> players)
        {
            ShowOnly(_revealPanel);
            _revealBannerText.text = bannerText;
            var top = players.OrderByDescending(p => p.score).Take(5);
            _revealScoresText.text = string.Join("\n", top.Select((p, i) => $"{i + 1}. {p.name} — {p.score} ({(p.delta > 0 ? "+" + p.delta : "0")})"));
        }

        private void ShowFinalUI(List<PlayerPublic> sorted)
        {
            ShowOnly(_finalPanel);
            _finalScoresText.text = string.Join("\n", sorted.Select((p, i) => $"{i + 1}. {p.name} — {p.score}"));
        }
    }
}
