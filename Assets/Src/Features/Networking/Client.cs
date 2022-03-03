using UnityEngine;
using UnityEngine.Networking;

namespace Src.Features.Networking
{
    public class Client
    {
        private int _reliableChannelIndex;
        private int _unreliableChannelIndex;
        private int _hostId;
        private int _connectionId;

        private bool _isConnected;
        public bool IsConnected => _isConnected;

        public void Connect(string address, int port)
        {
            NetworkTransport.Init();
            ConnectionConfig config = new ConnectionConfig();
            _reliableChannelIndex = config.AddChannel(QosType.Reliable);
            _unreliableChannelIndex = config.AddChannel(QosType.Unreliable);
            HostTopology topology = new HostTopology(config, 10);
            _hostId = NetworkTransport.AddHost(topology, port);
            _connectionId = NetworkTransport.Connect(_hostId, address, port, 0, out byte error);
            NetworkError networkError = (NetworkError) error;
            if (networkError == NetworkError.Ok)
                _isConnected = true;
            else
                Debug.Log(networkError);
        }

        public void Disconnect()
        {
            if (!_isConnected)
                return;
            if (!NetworkTransport.Disconnect(_hostId, _connectionId, out byte error))
                return;
            NetworkError networkError = (NetworkError) error;
            if (networkError == NetworkError.Ok)
                _isConnected = false;
            else
                Debug.Log(networkError);
        }
    }
}
