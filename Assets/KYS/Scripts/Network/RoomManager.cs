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
            startButton.onClick.AddListener(GameStart);
            leaveButton.onClick.AddListener(LeaveRoom);
            mapLeftButton.onClick.AddListener(ClickLeftMapButton);
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
                startButton.interactable = true;
                mapLeftButton.interactable = true;
                mapRightButton.interactable = true;
                panel.Init(player);
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
                startButton.interactable = false;
                mapLeftButton.interactable = false;
                mapRightButton.interactable = false;
                MapChange();

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
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                Destroy(playerPanels[player.ActorNumber].gameObject);
            }

            playerPanels.Clear();

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
            mapIndex = (int)PhotonNetwork.CurrentRoom.CustomProperties["Map"];
            Debug.Log(mapIndex);
            mapImage.sprite = mapSprites[mapIndex];
        }

    }
}