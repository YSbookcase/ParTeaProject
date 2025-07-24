using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace KYS
{
    public class ChatManager : MonoBehaviourPun
    {
        [SerializeField] private TMP_InputField chatField;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private GameObject chatTextPrefab;
        public Transform ChatContent;

        void Start()
        {
            chatField.onEndEdit.AddListener(HandleInput);
        }

        private void HandleInput(string text)
        {
            if (!Input.GetKeyDown(KeyCode.Return))
                return;

            if (!string.IsNullOrWhiteSpace(text))
            {
                photonView.RPC("SendMessage", RpcTarget.All, PhotonNetwork.NickName, text);
                chatField.text = "";
                chatField.ActivateInputField();
            }
        }

        [PunRPC]
        private void SendMessage(string sender, string message)
        {
            GameObject item = Instantiate(chatTextPrefab, ChatContent);
            item.GetComponent<TextMeshProUGUI>().text = $"{sender} : {message}";
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }

        public void ClearChat()
        {
            if (ChatContent != null)
            {
                while (ChatContent.childCount > 0)
                {
                    Transform child = ChatContent.GetChild(0);
                    if (child != null)
                    {
                        DestroyImmediate(child.gameObject);
                    }
                }
            }
        }
    }
}