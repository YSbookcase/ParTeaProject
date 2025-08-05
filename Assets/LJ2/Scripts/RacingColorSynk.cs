using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine;
using ExitGames.Client.Photon;

public class RacingColorSynk : MonoBehaviourPunCallbacks
{
    [SerializeField] private Renderer playerRenderer;
    [SerializeField] private RacingController racingController;

    // ArenaPlayerSpawner, Color와 동일한 색상 배열
    private Color[] colors = {
            Color.red,
            Color.blue,
            Color.green,
            Color.yellow
        };

    [SerializeField] List<GameObject> carryFoods = new List<GameObject>();
    [SerializeField] private AudioData audioData;

    private void Start()
    {
        ApplyColor();
    }

    private void ApplyColor()
    {
        if (photonView.Owner != null && photonView.Owner.CustomProperties.ContainsKey("Color"))
        {
            int colorIndex = (int)photonView.Owner.CustomProperties["Color"];
            playerRenderer.materials[0].color = colors[colorIndex];
            for (int i = 0; i < carryFoods.Count; i++)
            {
                if(i == colorIndex)
                {
                    carryFoods[i].SetActive(true);
                    racingController.SetRacingSound(audioData.clip.name, i);
                }
                else
                {
                    carryFoods[i].SetActive(false);
                }
            }
        }
    }

    // 새로운 플레이어가 들어와서 프로퍼티가 업데이트되면 다시 색상을 적용
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (targetPlayer == photonView.Owner && changedProps.ContainsKey("Color"))
        {
            ApplyColor();
        }
    }
}

