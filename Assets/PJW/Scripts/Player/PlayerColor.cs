using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace PJW
{
    [RequireComponent(typeof(Renderer))]
    public class PlayerColor : MonoBehaviourPunCallbacks
    {
        [Header("색상 팔레트")]
        [SerializeField] private Color[] colorPalette = new Color[4];

        private Renderer bodyRenderer;

        private void Awake()
        {
            bodyRenderer = GetComponent<Renderer>();
        }

        private void Start()
        {
            ApplyColor();
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            if (targetPlayer == photonView.Owner && changedProps.ContainsKey("Color"))
            {
                ApplyColor();
            }
        }

        private void ApplyColor()
        {
            if (!photonView.Owner.CustomProperties.TryGetValue("Color", out object value))
                return;

            int idx = (value is int i) ? i : -1;
            if (idx < 0 || idx >= colorPalette.Length)
            {
                Debug.LogWarning($"[PlayerColorFetcher] 잘못된 색상 인덱스: {idx}");
                return;
            }

            Color selected = colorPalette[idx];

            // 몸체 머티리얼 컬러 교체
            if (bodyRenderer != null)
                bodyRenderer.material.color = selected;
        }
    }
}
