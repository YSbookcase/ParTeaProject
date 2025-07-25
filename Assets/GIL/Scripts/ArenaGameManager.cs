using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class ArenaGameManager : MonoBehaviourPunCallbacks
{
    public static ArenaGameManager Instance;

    private List<Player> alivePlayers = new List<Player>();
    private List<Player> rankList = new List<Player>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            foreach (var kvp in PhotonNetwork.CurrentRoom.Players)
            {
                alivePlayers.Add(kvp.Value);
            }
        }
    }

    /// <summary>
    /// 플레이어가 죽을 때 호출되는 함수
    /// </summary>
    public void PlayerDied(Player player)
    {
        Debug.Log("플레이어 사망!");
        if (!PhotonNetwork.IsMasterClient) return;

        if (alivePlayers.Contains(player))
        {
            alivePlayers.Remove(player);
            rankList.Insert(0, player);

            Debug.Log($"{player.NickName} 탈락! 남은 인원: {alivePlayers.Count}");

            if (alivePlayers.Count <= 1)
            {
                if (alivePlayers.Count == 1)
                    rankList.Insert(0, alivePlayers[0]);

                photonView.RPC(nameof(RPC_EndGame), RpcTarget.All, GetNickNameArray());
            }
        }
    }

    private string[] GetNickNameArray()
    {
        string[] names = new string[rankList.Count];
        for (int i = 0; i < rankList.Count; i++)
        {
            names[i] = rankList[i].NickName;
        }
        return names;
    }

    [PunRPC]
    private void RPC_EndGame(string[] nicknames)
    {
        Debug.Log("게임 종료! 최종 순위:");
        for (int i = 0; i < nicknames.Length; i++)
        {
            Debug.Log($"{i + 1}위: {nicknames[i]}");
        }

        SceneManager.LoadScene("Score");
    }
}