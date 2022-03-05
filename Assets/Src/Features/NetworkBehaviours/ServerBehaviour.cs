using Src.Features.Networking;
using UnityEngine;


namespace Src.Features.NetworkBehaviours
{
    public class ServerBehaviour: MonoBehaviour
    {
        [SerializeField] private int maxConnections;
        [SerializeField] private int port;
        [SerializeField] private bool isWebsocket;
        private Server _server;
        
        
        private void Start()
        {
            _server = new Server(maxConnections);
            _server.Start(port, false);
        }

        private void OnDestroy()
        {
            _server.Stop();
            _server.Dispose();
        }
    }
}