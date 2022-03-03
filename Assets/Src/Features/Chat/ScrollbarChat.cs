using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


namespace Src.Features.Chat
{
    [RequireComponent(typeof(Scrollbar))]
    public class ScrollbarChat : MonoBehaviour
    {
        [SerializeField] private InputField textInput;
        [SerializeField] private Text textChat;
        [SerializeField] private UnityEvent<string> onNeedSendMessage;
        private IList<string> _messages = new List<string>();
        private Scrollbar _scrollbar;

        public void OnNeedUpdateMessage(string message)
        {
            _messages.Add(message);

            float value = (_messages.Count - 1) * _scrollbar.value;
            _scrollbar.value = Mathf.Clamp(value, 0, 1);
            UpdateText();
        }

        public void OnClientConnected()
        {
            textInput.gameObject.SetActive(true);
            textChat.gameObject.SetActive(true);
        }

        public void OnClientDisconnected()
        {
            textInput.gameObject.SetActive(false);
            textChat.gameObject.SetActive(false);
        }

        private void Start()
        {
            OnClientDisconnected();
            _scrollbar = GetComponent<Scrollbar>();
            textInput.onEndEdit.AddListener(OnEndEditInput);
        }

        private void OnDestroy()
        {
            textInput.onEndEdit.RemoveListener(OnEndEditInput);
        }

        private void OnEndEditInput(string text)
        {
            onNeedSendMessage.Invoke(text);
        }

        private void UpdateText()
        {
            string text = "";

            int index = (int) (_messages.Count * _scrollbar.value);
            for (int i = index; i<_messages.Count; i++)
            {
                text += _messages[i] + "\n";
            }
            textChat.text = text;
        }
    }

}