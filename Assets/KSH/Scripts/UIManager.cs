using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
namespace KSH
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI redText;
        [SerializeField] private TextMeshProUGUI blueText;

        void Start()
        {
            if (TileManager.Instance != null)
                TileManager.Instance.OnTileCount += TileUIUpdate;
        }

        void OnDisable()
        {
            if(TileManager.Instance != null)
                TileManager.Instance.OnTileCount -= TileUIUpdate;
        }

        private void TileUIUpdate(int red, int blue)
        {
            redText.text = $"RedTeam : {red}";
            blueText.text = $"BlueTeam : {blue}";
        }
    }
}
