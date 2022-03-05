using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


namespace Src.Features.NetworkBehaviours
{
    public class UserInfoBehaviour: MonoBehaviour
    {
        [SerializeField] private UnityEvent<string> onConnectedUser;
        [SerializeField] private InputField userNameField;
        [SerializeField] private Button connectedButton;
        private string _userName;

        public void OnUserConnected()
        {
            userNameField.gameObject.SetActive(false);
            connectedButton.gameObject.SetActive(false);
        }

        public void OnUserDisconnected()
        {
            userNameField.gameObject.SetActive(true);
            connectedButton.gameObject.SetActive(true);
        }
        
        private void Start()
        {
            userNameField.onValueChanged.AddListener(OnChangedUserName);
            connectedButton.onClick.AddListener(OnConnectedClick);
        }

        private void OnDestroy()
        {
            userNameField.onValueChanged.RemoveListener(OnChangedUserName);
            connectedButton.onClick.RemoveListener(OnConnectedClick);
        }

        private void OnChangedUserName(string name)
        {
            _userName = name;
        }

        private void OnConnectedClick()
        {
            onConnectedUser.Invoke(_userName);
        }
    }
}