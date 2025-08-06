using Photon.Pun;
using UnityEngine;

namespace KSH
{
    public class Tile : MonoBehaviour
    {
        private Color curColor;
        private bool isBgm = false;

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
            if (!GameManager.Instance.isGameStart) return; //게임 시작이 되지않으면 충돌 판정 X
            
            if (!isBgm)
            {
                Manager.Audio.SfxPlay("KSH_Walk");
                isBgm = true;
            }
            isBgm = false;
            
            PlayerController player = other.gameObject.GetComponent<PlayerController>();

            int team = 0;
            if (player.photonView.Owner.CustomProperties.ContainsKey("Team"))
            {
                team = (int)player.photonView.Owner.CustomProperties["Team"];
            }
            else
            {
                return;
            }
            
            Color color = (team == 0) ? Color.red : Color.blue;
            if (color == curColor) return;
            
            string hexcolor = $"#{ColorUtility.ToHtmlStringRGB(color)}"; //플레이이의 색을 문자열로 변환
            PhotonView photonView = GetComponent<PhotonView>();
            if(photonView != null) //포톤뷰가 null이 아니면
                photonView.RPC("SetColor", RpcTarget.AllBuffered, hexcolor); //RPC 호출
        }
    }    
}
