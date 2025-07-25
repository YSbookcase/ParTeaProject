using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class ArenaMatchingTest : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text roomNameText;
    
    [Header("Player Prefab")]
    [SerializeField] private GameObject playerPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnRadius = 5f;
    [SerializeField] private Vector3 center = Vector3.zero;
    [SerializeField] private GameObject manager;
    private bool _hasSpawnedPlayer;

    private void Awake()
    {
        if (PhotonNetwork.IsConnected == false)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinRandomOrCreateRoom();
    }

    public override void OnJoinedRoom()
    {
        panel.SetActive(false);
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;
        
        TrySpawnPlayer();
    }
    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        TrySpawnPlayer(); // 누군가 새로 들어오면 검사
    }
    
    private void TrySpawnPlayer()
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount >= 2 && !_hasSpawnedPlayer)
        {
            SpawnPlayer();
            manager.SetActive(true);
            _hasSpawnedPlayer = true;
        }
    }

    private void SpawnPlayer()
    {
        int playerIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;
        int totalPlayers = PhotonNetwork.CurrentRoom.PlayerCount;

        float angle = 360f / totalPlayers * playerIndex;
        float rad = angle * Mathf.Deg2Rad;
        Vector3 spawnPos = center + new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad)) * spawnRadius;

        PhotonNetwork.Instantiate(playerPrefab.name, spawnPos, Quaternion.identity);
    }
}
