using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class ArenaPlayerSpawner : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private float spawnRadius = 5f;
    [SerializeField] private Vector3 center = Vector3.zero;

    private int _playerCount;
    
    private void OnEnable()
    {
        SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        _playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        int myIndex = GetPlayerIndex();

        // 각도를 등분함
        float anglePerPlayer = 360f / _playerCount;
        float angle = anglePerPlayer * myIndex;
        float rad = angle * Mathf.Deg2Rad;

        // 위치 계산 (Y는 0 또는 원하는 높이)
        Vector3 spawnPos = new Vector3(
            center.x + Mathf.Cos(rad) * spawnRadius,
            center.y,
            center.z + Mathf.Sin(rad) * spawnRadius
        );

        // 생성
        PhotonNetwork.Instantiate(playerPrefab.name, spawnPos, Quaternion.identity);
    }

    private int GetPlayerIndex()
    {
        var players = PhotonNetwork.CurrentRoom.Players;
        int index = 0;
        foreach (var kvp in players)
        {
            if (kvp.Value == PhotonNetwork.LocalPlayer)
                return index;
            index++;
        }
        return 0; // fallback
    }
}
