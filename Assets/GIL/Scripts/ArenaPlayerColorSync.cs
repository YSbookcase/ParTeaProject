using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace GIL.Scripts
{
    public class ArenaPlayerColorSync : MonoBehaviourPunCallbacks
    {
        [SerializeField] private Renderer ballRenderer;
        [SerializeField] private MeshFilter[] playerMeshRenderer;
        [SerializeField] private MeshFilter playerMeshFilter;
        [SerializeField] private Outline playerOutline;
        // ArenaPlayerSpawner, Color와 동일한 색상 배열
        [SerializeField] private Color[] colors = {
            Color.red,
            Color.blue,
            Color.green,
            Color.yellow
        };

        private void Start()
        {
            ApplyColorAndMesh();
        }

        private void ApplyColorAndMesh()
        {
            if (photonView.Owner != null && photonView.Owner.CustomProperties.ContainsKey("Color"))
            {
                int colorIndex = (int)photonView.Owner.CustomProperties["Color"];
                playerOutline.OutlineColor = colors[colorIndex];
                
                if (colorIndex >= 0 && colorIndex < playerMeshRenderer.Length)
                {
                    playerMeshFilter.mesh = playerMeshRenderer[colorIndex].sharedMesh;
                }
            }
            
            if (PhotonNetwork.IsConnected == false)
            {
                int randIndex = Random.Range((int)0, (int)3);
                playerOutline.OutlineColor = colors[randIndex];
                playerMeshFilter.mesh = playerMeshRenderer[randIndex].sharedMesh;
            }
        }
        
        // 새로운 플레이어가 들어와서 프로퍼티가 업데이트되면 다시 색상을 적용
        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            if (targetPlayer == photonView.Owner && changedProps.ContainsKey("Color"))
            {
                ApplyColorAndMesh();
            }
        }
    }
}