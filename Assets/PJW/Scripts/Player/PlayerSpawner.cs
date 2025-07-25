using Photon.Pun;
using UnityEngine;

namespace PJW
{
    public class PlayerSpawner : MonoBehaviourPunCallbacks
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform[] spawnPoints;

        [PunRPC]
        private void RopeGameSpawnPlayer(int actorNumber)
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber != actorNumber) return;

            int index = actorNumber - 1;
            Vector3 spawnPos = spawnPoints[index].position;
            PhotonNetwork.Instantiate(playerPrefab.name, spawnPos, Quaternion.identity);
        }

        public override void OnJoinedRoom()
        {
            photonView.RPC(nameof(RopeGameSpawnPlayer), RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }
}
