using Src.Features.Networking;
using UnityEngine;
using UnityEngine.Events;


namespace Src.Features.NetworkBehaviours
{
    public class ClientBehaviour: MonoBehaviour
    {
        [SerializeField] private UnityEvent<string> onMessageReceivedEvent;
        [SerializeField] private UnityEvent onClientConnected;
        [SerializeField] private UnityEvent onClientDisconnected;
        [SerializeField] private string address;
        [SerializeField] private int port;
        [SerializeField] private bool isWebsocket;
        private Client _client;

        private void Start()
        {
            _client = new Client(256);
            _client.onMessageReceived += MessageReceived;
            _client.onClientConnected += onClientConnected.Invoke;
            _client.onClinetDisconnected += onClientDisconnected.Invoke;
        }

        public void Connect(string userName)
        {
            _client.Connect(address, port, isWebsocket, userName);
        }

        public void Disconnect()
        {
            _client.Disconnect();
        }

        public void SendMessage(string message)
        {
            _client.SendMessage(message, true);
        }

        private void OnDestroy()
        {
            _client.onMessageReceived -= MessageReceived;
            _client.onClientConnected -= onClientConnected.Invoke;
            _client.onClinetDisconnected -= onClientDisconnected.Invoke;
            _client.Disconnect();
            _client.Dispose();
        }

        private void MessageReceived(object message)
        {
            onMessageReceivedEvent.Invoke(message.ToString());
        }
    }
}