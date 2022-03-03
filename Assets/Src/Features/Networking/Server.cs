using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Src.Features.Networking
{
    public class Server: IDisposable
    {
        private CancellationTokenSource _cancellationTokenSource;
        private int _maxConnection;
        private int _reliableChannel;
        private int _unreliableChannel;
        private int _hostId;
        private bool _isStarted;
        private IDictionary<int, string> _connectedUsers = new Dictionary<int, string>();
        
        public Server(int networkClientMaxConnection)
        {
            _maxConnection = networkClientMaxConnection;
        }

        public void Start(int port)
        {
            NetworkTransport.Init();
            ConnectionConfig cc = new ConnectionConfig();
            _reliableChannel = cc.AddChannel(QosType.Reliable);
            _unreliableChannel = cc.AddChannel(QosType.Unreliable);
            HostTopology topology = new HostTopology(cc, _maxConnection);
            _hostId = NetworkTransport.AddHost(topology, port);
            _isStarted = true;
            _cancellationTokenSource = new CancellationTokenSource();
            ReceiveMessageWaiter(_cancellationTokenSource.Token);
        }

        public void Stop()
        {
            if (!_isStarted) return;

            NetworkTransport.RemoveHost(_hostId);
            NetworkTransport.Shutdown();
            _cancellationTokenSource.Cancel();
            _isStarted = false;
        }
        
        public void SendMessage(string message, int connectionID, bool isReliable)
        {
            byte[] buffer = Encoding.Unicode.GetBytes(message);
            NetworkTransport.Send(_hostId, connectionID, isReliable ? _reliableChannel : _unreliableChannel, buffer, message.Length * sizeof(char), out byte error);
            #if UNITY_EDITOR
            if ((NetworkError)error != NetworkError.Ok) Debug.Log((NetworkError)error);
            #endif
        }
        
        public void SendMessageToAll(string message, bool isReliable)
        {
            int[] ids = _connectedUsers.Keys.ToArray();
            
            for (int i = 0; i < ids.Length; i++)
            {
                SendMessage(message, ids[i], isReliable);
            }
        }

        private async void ReceiveMessageWaiter(CancellationToken token)
        {
            if (!_isStarted) return;

            int recHostId;
            int connectionId;
            int channelId;
            byte[] recBuffer = new byte[1024];
            int bufferSize = 1024;
            int dataSize;
            
            while (!token.IsCancellationRequested)
            {
                NetworkEventType recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId,
                    recBuffer, bufferSize, out dataSize, out byte error);
                switch (recData)
                {
                    case NetworkEventType.Nothing:
                        break;

                    case NetworkEventType.ConnectEvent:
                        _connectedUsers.Add(connectionId, string.Empty);
                        break;

                    case NetworkEventType.DataEvent:
                        string message = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                        if (string.IsNullOrEmpty(_connectedUsers[connectionId]))
                        {
                            _connectedUsers[connectionId] = message;
                            SendMessageToAll($"{message} enter the chat", true);
                            break;
                        }

                        SendMessageToAll($"{_connectedUsers[connectionId]}: {message}", true);
                        break;

                    case NetworkEventType.DisconnectEvent:
                        SendMessageToAll($"{_connectedUsers[connectionId]} has disconnected.", true);
                        _connectedUsers.Remove(connectionId);
                        break;

                    case NetworkEventType.BroadcastEvent:
                        break;
                }

                await Task.Yield();
            }
            token.ThrowIfCancellationRequested();
        }


        public void Dispose()
        {
            Stop();
        }
    }
}