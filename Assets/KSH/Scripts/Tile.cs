using System.Collections;
using System.Collections.Generic;
using KSH;
using Photon.Pun;
using UnityEngine;

namespace KSH
{
    public class Tile : MonoBehaviour
    {
        private Color curColor;

        [PunRPC]
        public void SetColor(string hexcolor)
        {
            Color newColor;
            
            if (ColorUtility.TryParseHtmlString(hexcolor, out newColor))
            {
                if (PhotonNetwork.IsMasterClient)
                    TileManager.Instance.CountTile(curColor, newColor);
                GetComponent<Renderer>().material.color = newColor;
                curColor = newColor;
            }
        }
        private void OnCollisionEnter(Collision other)
        {
            PlayerController player = other.gameObject.GetComponent<PlayerController>();
            
            Color color = player.color;
            if (color == curColor) return;
            
            string hexcolor = $"#{ColorUtility.ToHtmlStringRGB(color)}";
            PhotonView photonView = GetComponent<PhotonView>();
            if(photonView != null)
                photonView.RPC("SetColor", RpcTarget.AllBuffered, hexcolor);
        }
    }    
}
