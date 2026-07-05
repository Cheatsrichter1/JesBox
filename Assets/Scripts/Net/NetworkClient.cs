using System;
using System.Text;
using Newtonsoft.Json;
using NativeWebSocket;
using UnityEngine;

namespace JesBox.Net
{
    /// <summary>
    /// Thin wrapper around NativeWebSocket that the host (Unity/TV) uses to
    /// talk to the relay server. Every message is a JSON object with a
    /// "type" field; callers parse msg bodies themselves via OnTextMessage.
    /// </summary>
    public class NetworkClient : MonoBehaviour
    {
        public event Action OnOpen;
        public event Action<string> OnTextMessage;
        public event Action<string> OnError;
        public event Action OnClose;

        private WebSocket _socket;

        public bool IsOpen => _socket != null && _socket.State == WebSocketState.Open;

        public async void Connect(string url)
        {
            _socket = new WebSocket(url);
            _socket.OnOpen += () => OnOpen?.Invoke();
            _socket.OnMessage += (bytes) => OnTextMessage?.Invoke(Encoding.UTF8.GetString(bytes));
            _socket.OnError += (err) => OnError?.Invoke(err);
            _socket.OnClose += (code) => OnClose?.Invoke();
            await _socket.Connect();
        }

        public void SendJson(object payload)
        {
            if (!IsOpen) return;
            string json = JsonConvert.SerializeObject(payload);
            _socket.SendText(json);
        }

        private void Update()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            _socket?.DispatchMessageQueue();
#endif
        }

        private async void OnDestroy()
        {
            if (_socket != null) await _socket.Close();
        }

        private async void OnApplicationQuit()
        {
            if (_socket != null) await _socket.Close();
        }
    }
}
