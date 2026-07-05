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
    /// The TV-side host: owns the room, runs the trivia state machine, and
    /// builds its own UI at runtime. Drop this on a single empty GameObject
    /// in the scene and press Play — no other scene setup required.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Connection")]
        [SerializeField] private string serverUrl = "ws://localhost:8080/ws";

        [Header("Round settings")]
        [SerializeField] private float questionTimeLimit = 10f;
        [SerializeField] private float revealDuration = 5f;
        [SerializeField] private int questionsPerGame = 5;

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
        private string _roomCode = "----";
        private bool _gameRunning;
        private float _currentRemaining;

        // UI references
        private RectTransform _lobbyPanel, _questionPanel, _revealPanel, _finalPanel;
        private Text _lobbyCodeText, _lobbyPlayersText;
        private Button _startButton;
        private Text _questionHeaderText, _questionBodyText, _questionChoicesText, _questionTimerText;
        private Image _timerFillImage;
        private Text _revealCorrectText, _revealScoresText;
        private Text _finalScoresText;

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

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private void Start()
        {
            _net.Connect(serverUrl);
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
                    if (_gameRunning && game?.data != null && game.data.action == "answer"
                        && !_answersThisRound.ContainsKey(game.playerId))
                    {
                        _answersThisRound[game.playerId] = new Answer { Choice = game.data.choice, RemainingTime = _currentRemaining };
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

        // ---- Game flow ----

        private void OnStartClicked()
        {
            if (_gameRunning || _players.Count == 0) return;
            _gameRunning = true;
            _startButton.gameObject.SetActive(false);
            StartCoroutine(RunGame());
        }

        private IEnumerator RunGame()
        {
            var pool = new List<TriviaQuestion>(TriviaQuestions.All);
            Shuffle(pool);
            int count = Mathf.Min(questionsPerGame, pool.Count);

            for (int i = 0; i < count; i++)
            {
                yield return RunQuestion(pool[i], i, count);
            }

            var sorted = PublicList().OrderByDescending(p => p.score).ToList();
            _net.SendJson(new GameOut<FinalPayload> { data = new FinalPayload { players = sorted } });
            ShowFinalUI(sorted);
            _gameRunning = false;
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
            ShowRevealUI(q, publicList);

            yield return new WaitForSeconds(revealDuration);
        }

        private static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // ---- UI ----

        private void BuildUI()
        {
            var canvas = UIFactory.CreateCanvas("JesBoxCanvas");
            UIFactory.CreateFullStretchPanel(canvas.transform, "Background", UIFactory.BgDeep);

            _lobbyPanel = BuildLobbyPanel(canvas.transform);
            _questionPanel = BuildQuestionPanel(canvas.transform);
            _revealPanel = BuildRevealPanel(canvas.transform);
            _finalPanel = BuildFinalPanel(canvas.transform);
        }

        private RectTransform BuildLobbyPanel(Transform parent)
        {
            var panel = UIFactory.CreateFullStretchPanel(parent, "LobbyPanel", Color.clear);
            UIFactory.CreateText(panel, "JESBOX", 90, UIFactory.Gold, TextAnchor.MiddleCenter,
                new Vector2(0, 380), new Vector2(1200, 140), FontStyle.Bold);
            _lobbyCodeText = UIFactory.CreateText(panel, "ROOM CODE: ----", 64, UIFactory.Cream, TextAnchor.MiddleCenter,
                new Vector2(0, 220), new Vector2(1400, 100), FontStyle.Bold);
            UIFactory.CreateText(panel, $"Join at {JoinUrlHint()}", 32, UIFactory.Cream, TextAnchor.MiddleCenter,
                new Vector2(0, 140), new Vector2(1400, 60));
            _lobbyPlayersText = UIFactory.CreateText(panel, "Waiting for players...", 34, UIFactory.Cream, TextAnchor.MiddleCenter,
                new Vector2(0, -60), new Vector2(1200, 400));
            _startButton = UIFactory.CreateButton(panel, "Start Game", new Vector2(0, -380), new Vector2(360, 90));
            _startButton.onClick.AddListener(OnStartClicked);
            _startButton.gameObject.SetActive(false);
            return panel;
        }

        private RectTransform BuildQuestionPanel(Transform parent)
        {
            var panel = UIFactory.CreateFullStretchPanel(parent, "QuestionPanel", Color.clear);
            _questionHeaderText = UIFactory.CreateText(panel, "Question 1 / 5", 36, UIFactory.Gold, TextAnchor.MiddleCenter,
                new Vector2(0, 420), new Vector2(1000, 60));
            _questionBodyText = UIFactory.CreateText(panel, "", 56, UIFactory.Cream, TextAnchor.MiddleCenter,
                new Vector2(0, 260), new Vector2(1500, 200), FontStyle.Bold);
            _questionChoicesText = UIFactory.CreateText(panel, "", 40, UIFactory.Cream, TextAnchor.MiddleCenter,
                new Vector2(0, 20), new Vector2(1200, 320));
            _questionTimerText = UIFactory.CreateText(panel, "10", 70, UIFactory.Gold, TextAnchor.MiddleCenter,
                new Vector2(0, -320), new Vector2(200, 100), FontStyle.Bold);

            var barBgGo = new GameObject("TimerBarBg", typeof(Image));
            var barBgRt = barBgGo.GetComponent<RectTransform>();
            barBgRt.SetParent(panel, false);
            barBgRt.anchorMin = new Vector2(0.5f, 0.5f);
            barBgRt.anchorMax = new Vector2(0.5f, 0.5f);
            barBgRt.anchoredPosition = new Vector2(0, -420);
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
            _timerFillImage = fillImg;

            return panel;
        }

        private RectTransform BuildRevealPanel(Transform parent)
        {
            var panel = UIFactory.CreateFullStretchPanel(parent, "RevealPanel", Color.clear);
            _revealCorrectText = UIFactory.CreateText(panel, "", 48, UIFactory.Gold, TextAnchor.MiddleCenter,
                new Vector2(0, 300), new Vector2(1400, 120), FontStyle.Bold);
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
            return panel;
        }

        private void ShowOnly(RectTransform panel)
        {
            _lobbyPanel.gameObject.SetActive(panel == _lobbyPanel);
            _questionPanel.gameObject.SetActive(panel == _questionPanel);
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

        private void UpdateQuestionTimerUI()
        {
            _questionTimerText.text = Mathf.CeilToInt(_currentRemaining).ToString();
            if (_timerFillImage != null)
                _timerFillImage.fillAmount = questionTimeLimit <= 0 ? 0 : _currentRemaining / questionTimeLimit;
        }

        private void ShowRevealUI(TriviaQuestion q, List<PlayerPublic> players)
        {
            ShowOnly(_revealPanel);
            string[] letters = { "A", "B", "C", "D" };
            _revealCorrectText.text = $"Correct answer: {letters[q.CorrectIndex]}) {q.Choices[q.CorrectIndex]}";
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
