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
            
            Color color = player.color; //플레이어의 컬러 저장
            if (color == curColor) return;
            
            string hexcolor = $"#{ColorUtility.ToHtmlStringRGB(color)}"; //플레이이의 색을 문자열로 변환
            PhotonView photonView = GetComponent<PhotonView>();
            if(photonView != null) //포톤뷰가 null이 아니면
                photonView.RPC("SetColor", RpcTarget.AllBuffered, hexcolor); //RPC 호출
        }
    }    
}
