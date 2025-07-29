using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RacingSpawner : MonoBehaviour
{
    public static RacingSpawner Instance;

    [SerializeField] private RacingMap racingMap;

    private bool isSpawned = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SetStartLine();
    }

    private void Update()
    {
        if (isSpawned || !Manager.game.isAllPlayerLoaded()) return;
        PlayerSpawn();
    }

    private void SetStartLine()
    {
        int trackLength = Random.Range(1, racingMap.racingLines.Count - 1);
        racingMap.SetTrack(trackLength);
        racingMap.SetDollyCart(racingMap.startLine);
    }

    private void PlayerSpawn()
    {
        isSpawned = true;
        
        int playerIndex = 0;
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.IsLocal) 
            {
                break;
            }
            playerIndex++;
        }
        Transform spawnPos = racingMap.startLine.spawnPositions[playerIndex];
        PhotonNetwork.Instantiate("RacingPlayer", spawnPos.position, spawnPos.rotation);
    }
}
