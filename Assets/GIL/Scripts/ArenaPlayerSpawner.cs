using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
public class ArenaPlayerSpawner : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private float spawnRadius = 5f;
    [SerializeField] private Vector3 center = Vector3.zero;
    
    [Header("Debug Settings")]
    public float radius = 5f;
    public int segments = 60;
    [SerializeField] private Color circleColor = Color.cyan;
    [SerializeField] private Color centerLineColor = Color.red;

    private int _playerCount;
    
    private void Start()
    {
        SpawnPlayer();
    }
    
    private void SpawnPlayer()
    {
        _playerCount = PhotonNetwork.PlayerList.Length;
        int myIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;

        float anglePerPlayer = 360f / _playerCount;
        float angle = anglePerPlayer * myIndex;
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

        Vector3 prevPoint = center + new Vector3(Mathf.Cos(0), 0, Mathf.Sin(0)) * radius;
        for (int i = 1; i <= segments; i++)
        {
            float rad = Mathf.Deg2Rad * angleStep * i;
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad)) * radius;
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }

        Gizmos.color = centerLineColor;
        Gizmos.DrawLine(center + Vector3.left * 0.5f, center + Vector3.right * 0.5f);
        Gizmos.DrawLine(center + Vector3.forward * 0.5f, center + Vector3.back * 0.5f);
    }
}
