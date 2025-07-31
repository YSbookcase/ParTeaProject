using Photon.Pun;
using UnityEngine;
using Photon.Realtime;
namespace KSH
{
    public class TestNetwork : MonoBehaviourPunCallbacks
    {
        void Start()
        {
            if (!PhotonNetwork.IsConnected)
            {
                PhotonNetwork.ConnectUsingSettings();
            }
            PlayerSpawn();
        }

        public override void OnConnectedToMaster()
        {
            RoomOptions roomOptions = new RoomOptions();
            roomOptions.MaxPlayers = 4; 

            PhotonNetwork.JoinOrCreateRoom(
                "DefaultRoom",    
                roomOptions,      
                TypedLobby.Default
            );
        }

        public override void OnJoinedRoom()
        {
            Debug.Log("Joined Room");
            PhotonNetwork.LocalPlayer.NickName = $"Player_{PhotonNetwork.LocalPlayer.ActorNumber}";
        }

        private void PlayerSpawn()
        {
            Vector3 spawnPos = new Vector3(Random.Range(5, 48), 2, Random.Range(0, 14));
            GameObject player = PhotonNetwork.Instantiate("Player_KSH", spawnPos, Quaternion.identity);
        }
    }    
}
