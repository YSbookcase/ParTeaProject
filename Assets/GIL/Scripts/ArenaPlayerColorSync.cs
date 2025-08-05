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
        
        // ArenaPlayerSpawner, Color와 동일한 색상 배열
        [SerializeField] private Color[] colors = {
            new Color(1f, 0f, 0f, 0.4f),
            new Color(0f, 0f, 1f, 0.4f),
            new Color(0f, 1f, 0f, 0.4f), 
            new Color(1f, 1f, 0f, 0.4f)
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
                ballRenderer.material.color = colors[colorIndex];
                
                if (colorIndex >= 0 && colorIndex < playerMeshRenderer.Length)
                {
                    playerMeshFilter.mesh = playerMeshRenderer[colorIndex].sharedMesh;
                }
            }
            
            if (PhotonNetwork.IsConnected == false)
            {
                int randIndex = Random.Range((int)1, (int)4);
                playerMeshFilter.mesh = playerMeshRenderer[randIndex].sharedMesh;
                ballRenderer.material.color = colors[randIndex];
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