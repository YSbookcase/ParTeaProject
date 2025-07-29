using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using Photon.Realtime;

namespace KSH
{
    public class ColorManager : MonoBehaviourPunCallbacks
    {
        private Dictionary<int, PlayerController> playerControllers = new Dictionary<int, PlayerController>();

        public static ColorManager Instance;

        private void Awake()
        {
            if(Instance == null) // Instance가 null이면
            {
                Instance = this; // 할당
                DontDestroyOnLoad(gameObject); // 씬 전환에도 파괴되지 않도록 설정
            }
            else // 이미 존재한다면
            {
                Destroy(gameObject); // 하나만 존재해야 하므로 제거
            }
        }

        private void Start()
        {
            if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Team"))
            {
                int team = (int)PhotonNetwork.LocalPlayer.CustomProperties["Team"];
                SetColor(team);
            }
        }
        
        public void SetColor(int team)
        {
            Color teamColor = (team == 0) ? Color.red : Color.blue;

            string colorHex = ColorUtility.ToHtmlStringRGB(teamColor); //Color 타입을 문자열로 변환(커스텀 프로퍼티는 Color 구조체 저장 못함)
        
            //커스텀프로퍼티 해시테이블 생성
            ExitGames.Client.Photon.Hashtable colorProperty = new ExitGames.Client.Photon.Hashtable();
            colorProperty["TeamColor"] = colorHex; //Color 키에 변환한 문자열 저장
            PhotonNetwork.LocalPlayer.SetCustomProperties(colorProperty); //로컬 플레이어의 커스텀프로퍼티에 변경사항 적용
        }
        public void RegisterPlayer(PlayerController pc) //플레이어 등록
        {
            int actorNum = pc.photonView.Owner.ActorNumber; //포톤뷰를 가지고 있는 플레이어의 고유 번호 저장
            if (!playerControllers.ContainsKey(actorNum)) //만약 키가 등록되어있지 않으면
                playerControllers.Add(actorNum, pc); //고유 번호를 등록
        }

        //플레이어의 속성이 바뀔 때 업데이트 되는 기능
        public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            if (changedProps.ContainsKey("Team"))
            {
                int team = (int)changedProps["Team"];
                
                if (playerControllers.TryGetValue(targetPlayer.ActorNumber, out PlayerController pc))
                {
                    SetColor(team);
                }
            }
            
            if (changedProps.ContainsKey("TeamColor")) //만약 Color 키가 변경되었으면
            {
                string colorHex = (string)changedProps["TeamColor"];  //Color 키를 문자열로 저장
                if (ColorUtility.TryParseHtmlString("#" + colorHex, out Color newColor)) //문자열을 색상으로 변경할 수 있다면
                {
                    //만약 플레이어의 고유 번호를 얻는다면
                    if (playerControllers.TryGetValue(targetPlayer.ActorNumber, out PlayerController pc))
                    {
                        pc.SettingColor(newColor);
                    }
                }
            }
        }
    }    
}
