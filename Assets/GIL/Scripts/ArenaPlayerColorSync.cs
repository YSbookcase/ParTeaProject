using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class ArenaPlayerColorSync : MonoBehaviourPunCallbacks
{
    [SerializeField] private Renderer playerRenderer;

    // ArenaPlayerSpawner, Color와 동일한 색상 배열
    private Color[] colors = {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow
    };

    private void Start()
    {
        ApplyColor();
    }

    private void ApplyColor()
    {
        if (photonView.Owner != null && photonView.Owner.CustomProperties.ContainsKey("Color"))
        {
            int colorIndex = (int)photonView.Owner.CustomProperties["Color"];
            playerRenderer.material.color = colors[colorIndex];
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