using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class ArenaGameManager : MonoBehaviourPunCallbacks
{
    public static ArenaGameManager Instance;

    private List<Player> alivePlayers = new List<Player>();
    private int currentRank;
    
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
            currentRank = alivePlayers.Count;
        }
    }

    /// <summary>
    /// 플레이어가 죽을 때 호출되는 함수
    /// </summary>
    public void PlayerDied(Player player)
    {
        Debug.Log("플레이어 사망!");
        if (!PhotonNetwork.IsMasterClient) return;

        if (player == null)
        {
            Debug.LogError("PlayerDied: 전달된 player가 null임!");
            return;
        }

        Debug.Log($"PlayerDied: 대상 플레이어 ActorNumber = {player.ActorNumber}, NickName = {player.NickName}");

        if (alivePlayers.Contains(player))
        {
            alivePlayers.Remove(player);

            Debug.Log("alivePlayers에서 제거 완료");

            try
            {
                player.SetRank(currentRank);
                Debug.Log($"{player.NickName} 탈락! {currentRank}위 배정");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"SetRank 호출 중 오류: {ex}");
            }

            currentRank--;

            if (alivePlayers.Count <= 1)
            {
                if (alivePlayers.Count == 1) alivePlayers[0].SetRank(1);
                photonView.RPC(nameof(ArenaEndGame), RpcTarget.All);
            }
        }
        else
        {
            Debug.LogWarning($"alivePlayers 리스트에 없는 플레이어: {player.NickName}");
        }
    }

    [PunRPC]
    private void ArenaEndGame()
    {
        Debug.Log("게임 종료! 최종 순위:");
        foreach (var p in PhotonNetwork.PlayerList)
        {
            int rank = p.GetRank();
            Debug.Log($"{rank}위: {p.NickName}");
        }

        SceneManager.LoadScene("Score");
    }
}