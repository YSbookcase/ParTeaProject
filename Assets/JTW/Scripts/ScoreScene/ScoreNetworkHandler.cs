using JTW_JumpGame;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreNetworkHandler : MonoBehaviourPunCallbacks
{
    [SerializeField] private ScoreUIPresenter scorePresenter;

    private void Start()
    {
        if (isAllRankUpdated())
        {
            scorePresenter.InitScore();
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey("rank"))
        {
            if (isAllRankUpdated())
            {
                scorePresenter.InitScore();
            }
        }
    }

    private bool isAllRankUpdated()
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.ContainsKey("rank"))
            {
                if ((int)player.CustomProperties["rank"] == 0)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    public void GoNext()
    {
        photonView.RPC(nameof(GoNextGame), RpcTarget.All);
    }

    [PunRPC]
    private void GoNextGame()
    {
        Manager.game.GoNextMiniGame("JumpGame");
    }
}
