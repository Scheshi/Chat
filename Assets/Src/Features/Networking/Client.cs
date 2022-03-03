using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Src.Features.Networking
{
    /// <summary>
    /// Объект клиента подключения
    /// </summary>
    public class Client: IDisposable
    {
        public event Action<object> onMessageReceived = delegate(object o) {  };
        public event Action onClientConnected = delegate {  };
        public event Action onClientDisconnected = delegate {  };

        private CancellationTokenSource _cancellationTokenSource;
        private int _reliableChannelIndex;
        private int _unreliableChannelIndex;
        private int _hostId = -1;
        private int _connectionId = -1;
        private int _tickrate;
        private string _clientName;

        private bool _isConnected;
        public bool IsConnected => _isConnected;

        public Client(int tickRate)
        {
            _tickrate = tickRate;
        }

        /// <summary>
        /// Реализовать подключение к серверу
        /// </summary>
        /// <param name="address">Адрес удаленного хоста</param>
        /// <param name="port">Порт удаленного хоста</param>
        /// <param name="userName">Имя пользователя</param>
        public void Connect(string address, int port, bool isWebsocket = false, string userName = null)
        {
            _clientName = userName;
            NetworkTransport.Init();
            ConnectionConfig config = new ConnectionConfig();
            _reliableChannelIndex = config.AddChannel(QosType.Reliable);
            _unreliableChannelIndex = config.AddChannel(QosType.Unreliable);
            HostTopology topology = new HostTopology(config, 10);
            if (isWebsocket)
                _hostId = NetworkTransport.AddWebsocketHost(topology, 0, address);
            else
                _hostId = NetworkTransport.AddHost(topology, 0);
                
            _connectionId = NetworkTransport.Connect(_hostId, address, port, 0, out byte error);
            NetworkError networkError = (NetworkError) error;
            if (networkError == NetworkError.Ok)
            {
                _cancellationTokenSource = new CancellationTokenSource();
                ReceiveMessageWaiter(_cancellationTokenSource.Token);
            }
#if UNITY_EDITOR
            else
                Debug.Log(networkError);
            #endif
        }

        /// <summary>
        /// Реализовать отключение от сервера
        /// </summary>
        public void Disconnect()
        {
            if (!_isConnected)
                return;
            _cancellationTokenSource.Cancel();
            if (!NetworkTransport.Disconnect(_hostId, _connectionId, out byte error))
                return;
            NetworkError networkError = (NetworkError) error;
            if (networkError == NetworkError.Ok)
                _isConnected = false;
            #if UNITY_EDITOR
            else
                Debug.Log(networkError);
            #endif
        }

        /// <summary>
        /// Отправить сообщение
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="isReliable">Гарантированный метод доставки</param>
        public void SendMessage(string message, bool isReliable)
        {
            byte[] buffer = Encoding.Unicode.GetBytes(message);
            if (NetworkTransport.Send(_hostId, _connectionId,
                (isReliable ? _reliableChannelIndex : _unreliableChannelIndex), buffer, message.Length * sizeof(char),
                out byte error))
                return;
#if UNITY_EDITOR
            NetworkError networkError = (NetworkError) error;
            if(networkError != NetworkError.Ok)
                Debug.Log(networkError);
#endif
        }


        private byte[] recBuffer = new byte[1024];
        private async void ReceiveMessageWaiter(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                NetworkEventType ev = NetworkTransport.Receive(out int hostId, out int connectionId, out int channelId,
                    recBuffer, recBuffer.Length, out int recSize, out byte error);
                switch (ev)
                {
                    case NetworkEventType.ConnectEvent:
                        onClientConnected.Invoke();
                        if (!_isConnected)
                        {
                            if(!string.IsNullOrEmpty(_clientName))
                                SendMessage(_clientName, true);
                            _isConnected = true;
                        }

                        break;
                    case NetworkEventType.DisconnectEvent:
                        onClientDisconnected.Invoke();
                        break;
                    case NetworkEventType.DataEvent:
                        string message = Encoding.Unicode.GetString(recBuffer, 0, recBuffer.Length);
                        onMessageReceived.Invoke(message);
                        break;
                    case NetworkEventType.Nothing:
                    case NetworkEventType.BroadcastEvent:
                    default:
                        break;
                }
                await Task.Yield();
            }
        }


        public void Dispose()
        {
            if(!_cancellationTokenSource.IsCancellationRequested)
                _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }
    }
}
