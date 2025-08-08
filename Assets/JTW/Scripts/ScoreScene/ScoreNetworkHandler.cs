using JTW_JumpGame;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ScoreNetworkHandler : MonoBehaviourPunCallbacks
{
    [SerializeField] private ScoreUIPresenter scorePresenter;
    [SerializeField] private Button nextButton;

    private bool isInit = false;

    private void Start()
    {
        if (isAllRankUpdated() && !isInit)
        {
            scorePresenter.InitScore();
            isInit = true;
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey("rank"))
        {
            if (isAllRankUpdated() && !isInit)
            {
                scorePresenter.InitScore();
                isInit = true;
            }
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (!newMasterClient.IsLocal) return;

        nextButton.interactable = true;
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
        Manager.game.GoNextMiniGame(null);
    }
}
