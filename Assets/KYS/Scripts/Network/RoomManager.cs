using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace KYS
{

    public class RoomManager : MonoBehaviour
    {

        [SerializeField] private Button startButton;
        [SerializeField] private Button leaveButton;
        [SerializeField] private Button mapLeftButton;
        [SerializeField] private Button mapRightButton;
        [SerializeField] private Image mapImage;
        [SerializeField] private Sprite[] mapSprites;
        [SerializeField] private GameObject playerPanelItemPrefabs;
        [SerializeField] private Transform playerPanelContent;

        public int mapIndex;
        public Dictionary<int, PlayerPanelItem> playerPanels = new Dictionary<int, PlayerPanelItem>();

        private void Start()
        {
            // null 체크 추가
            if (startButton != null)
                startButton.onClick.AddListener(GameStart);
            if (leaveButton != null)
                leaveButton.onClick.AddListener(LeaveRoom);
            if (mapLeftButton != null)
                mapLeftButton.onClick.AddListener(ClickLeftMapButton);
            if (mapRightButton != null)
                mapRightButton.onClick.AddListener(ClickRightMapButton);
        }

        //public void LateUpdate()
        //{
        //    foreach (Player player in PhotonNetwork.PlayerList)
        //    {
        //        Destroy(playerPanels[player.ActorNumber].gameObject);
        //    }
        //
        //    playerPanels.Clear();
        //
        //    PhotonNetwork.LeaveRoom();
        //}


        public void PlayerPanelSpawn(Player player)
        {
            if (playerPanels.TryGetValue(player.ActorNumber, out PlayerPanelItem panel))
            {
                if (startButton != null) startButton.interactable = true;
                if (mapLeftButton != null) mapLeftButton.interactable = true;
                if (mapRightButton != null) mapRightButton.interactable = true;
                panel.Init(player);
                return;
            }

            if (playerPanelItemPrefabs == null || playerPanelContent == null)
            {
                Debug.LogError("[RoomManager] playerPanelItemPrefabs 또는 playerPanelContent가 null입니다.");
                return;
            }

            GameObject obj = Instantiate(playerPanelItemPrefabs);
            obj.transform.SetParent(playerPanelContent);
            PlayerPanelItem item = obj.GetComponent<PlayerPanelItem>();
            item.Init(player);
            playerPanels.Add(player.ActorNumber, item);
        }

        public void GameStart()
        {
            if (PhotonNetwork.IsMasterClient && AllPlayerReadyCheck())
            {
                PhotonNetwork.LoadLevel("GameScene");
            }
        }

        public bool AllPlayerReadyCheck()
        {
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (!player.CustomProperties.TryGetValue("Ready", out object value) || !(bool)value)
                    return false;
            }

            return true;
        }

        public void PlayerPanelSpawn()
        {
            PhotonNetwork.AutomaticallySyncScene = true;

            if (!PhotonNetwork.IsMasterClient)
            {
                if (startButton != null) startButton.interactable = false;
                if (mapLeftButton != null) mapLeftButton.interactable = false;
                if (mapRightButton != null) mapRightButton.interactable = false;
                MapChange();
            }

            if (playerPanelItemPrefabs == null || playerPanelContent == null)
            {
                Debug.LogError("[RoomManager] playerPanelItemPrefabs 또는 playerPanelContent가 null입니다.");
                return;
            }

            foreach (Player player in PhotonNetwork.PlayerList)
            {
                GameObject obj = Instantiate(playerPanelItemPrefabs);
                obj.transform.SetParent(playerPanelContent);
                PlayerPanelItem item = obj.GetComponent<PlayerPanelItem>();
                item.Init(player);
                playerPanels.Add(player.ActorNumber, item);
            }
        }

        public void PlayerPanelDestory(Player player)
        {
            if (playerPanels.TryGetValue(player.ActorNumber, out PlayerPanelItem panel))
            {
                Destroy(panel.gameObject);
                playerPanels.Remove(player.ActorNumber);
            }
            else
            {
                Debug.LogError("패널이 존재하지 않음");
            }
        }

        public void LeaveRoom()
        {
            if (playerPanels != null)
            {
                foreach (Player player in PhotonNetwork.PlayerList)
                {
                    if (playerPanels.TryGetValue(player.ActorNumber, out PlayerPanelItem panel) && panel != null)
                    {
                        Destroy(panel.gameObject);
                    }
                }
                playerPanels.Clear();
            }

            PhotonNetwork.LeaveRoom();
        }



        public void ClickLeftMapButton()
        {
            mapIndex--;
            if (mapIndex == -1)
            {
                mapIndex = mapSprites.Length - 1;

            }

            ExitGames.Client.Photon.Hashtable roomProperty = new ExitGames.Client.Photon.Hashtable();
            roomProperty["Map"] = mapIndex;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperty);

            MapChange();
        }

        public void ClickRightMapButton()
        {
            mapIndex++;
            if (mapIndex == mapSprites.Length)
            {
                mapIndex = 0;

            }

            ExitGames.Client.Photon.Hashtable roomProperty = new ExitGames.Client.Photon.Hashtable();
            roomProperty["Map"] = mapIndex;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperty);

            MapChange();
        }


        public void MapChange()
        {
            if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("Map"))
            {
                mapIndex = (int)PhotonNetwork.CurrentRoom.CustomProperties["Map"];
                Debug.Log(mapIndex);
                
                if (mapImage != null && mapSprites != null && mapIndex >= 0 && mapIndex < mapSprites.Length)
                {
                    mapImage.sprite = mapSprites[mapIndex];
                }
            }
        }

    }
}