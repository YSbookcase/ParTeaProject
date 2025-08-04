using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace GIL.Scripts
{
    public class ArenaPlayerSpawner : MonoBehaviourPunCallbacks
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private float spawnRadius = 5f;
        [SerializeField] private Vector3 center = Vector3.zero;
    
        [Header("Debug Settings")]
        public int segments = 60;
        [SerializeField] private Color circleColor = Color.cyan;
        [SerializeField] private Color centerLineColor = Color.red;

        private int _playerCount;
        private int _playerNum;
        private void Start()
        {
            SpawnPlayer();
        }
        
        private void ResetActorNumber()
        {
            _playerNum = 0;
            foreach(Player player in PhotonNetwork.PlayerList)
            {
                if (player.IsLocal)
                {
                    break;
                }
                _playerNum++;
            }
        }
        
        private void SpawnPlayer()
        {
            _playerCount = PhotonNetwork.PlayerList.Length;
            ResetActorNumber();

            float anglePerPlayer = 360f / _playerCount;
            float angle = anglePerPlayer * _playerNum;
            float rad = angle * Mathf.Deg2Rad;

            Vector3 spawnPos = new Vector3(
                center.x + Mathf.Cos(rad) * spawnRadius,
                center.y,
                center.z + Mathf.Sin(rad) * spawnRadius
            );
        
            GameObject playerObj = PhotonNetwork.Instantiate(playerPrefab.name, spawnPos, Quaternion.identity);
        }
    
        private void OnDrawGizmos()
        {
            Gizmos.color = circleColor;
            float angleStep = 360f / segments;

            Vector3 prevPoint = center + new Vector3(Mathf.Cos(0), 0, Mathf.Sin(0)) * spawnRadius;
            for (int i = 1; i <= segments; i++)
            {
                float rad = Mathf.Deg2Rad * angleStep * i;
                Vector3 nextPoint = center + new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad)) * spawnRadius;
                Gizmos.DrawLine(prevPoint, nextPoint);
                prevPoint = nextPoint;
            }

            Gizmos.color = centerLineColor;
            Gizmos.DrawLine(center + Vector3.left * 0.5f, center + Vector3.right * 0.5f);
            Gizmos.DrawLine(center + Vector3.forward * 0.5f, center + Vector3.back * 0.5f);
        }
    }
}
