using Src.Features.Networking;
using UnityEngine;


namespace Src.Features.NetworkBehaviours
{
    public class ServerBehaviour: MonoBehaviour
    {
        [SerializeField] private int maxConnections;
        [SerializeField] private int port;
        private Server _server;
        
        
        private void Start()
        {
            _server = new Server(maxConnections);
            _server.Start(port);
        }

        private void OnDestroy()
        {
            _server.Stop();
        }
    }
}