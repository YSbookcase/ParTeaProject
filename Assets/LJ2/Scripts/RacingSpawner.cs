using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RacingSpawner : MonoBehaviour
{
    public static RacingSpawner Instance;

    [SerializeField] private RacingMap racingMap;

    private bool isSetStartLine = false;
    private bool isSetStartLineIndex = false;
    private bool isSpawned = false;

    private int trackLength;
    private int startIndex;

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
        
    }


    private void Update()
    {
        if (PhotonNetwork.IsMasterClient && Manager.game.isAllPlayerLoaded() && !isSetStartLineIndex)
        {
            SetStartLineIndex();
        }
        if (PhotonNetwork.IsMasterClient && isSetStartLineIndex && !isSetStartLine)
        {
            RacingManager.Instance.managerView.RPC("SetStartLine", RpcTarget.All, trackLength, startIndex);

            isSetStartLineIndex = true;
        }
        if (isSpawned || !Manager.game.isAllPlayerLoaded() || !isSetStartLine) return;
        PlayerSpawn();
    }

    public void SetStartLineIndex()
    {
        trackLength = Random.Range(1, racingMap.racingLines.Count - 1);
        startIndex = Random.Range(0, racingMap.racingLines.Count);
    }
    [PunRPC]
    public void SetStartLine(int trackLength, int startIndex)
    {
        racingMap.mapView.RPC("SetTrack", RpcTarget.All, trackLength, startIndex);
        racingMap.mapView.RPC("SetDollyCart", RpcTarget.All, startIndex);
        isSetStartLine = true;
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
