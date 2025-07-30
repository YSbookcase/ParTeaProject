using Photon.Pun;
using UnityEngine;
using PJW;          

namespace PJW
{
    public class PlayerNicknameUI : MonoBehaviourPun
    {
        [Header("닉네임 패널 Prefab")]
        [SerializeField] private NicknamePanel nicknamePanelPrefab;

        private Transform uiRootTransform;
        private NicknamePanel nicknamePanelInstance;

        private void Start()
        {
            // 씬에서 Canvas를 찾아서 할당
            GameObject canvasRoot = GameObject.Find("NicknameCanvasRoot");
            if (canvasRoot != null)
                uiRootTransform = canvasRoot.transform;
            else
            {
                Debug.LogError("씬에 'NicknameCanvasRoot' Canvas가 없습니다.");
                return;
            }

            // 패널 인스턴스화
            nicknamePanelInstance = Instantiate(nicknamePanelPrefab, uiRootTransform);

            // 닉네임과 플레이어 정보 전달
            string playerName = photonView.Owner != null
                ? photonView.Owner.NickName
                : "Unknown";

            var pc = GetComponent<PlayerController>();
            if (pc != null)
                nicknamePanelInstance.SetInfo(playerName, pc);
            else
                nicknamePanelInstance.SetInfo(playerName, transform);
        }

        private void OnDestroy()
        {
            if (nicknamePanelInstance != null)
                Destroy(nicknamePanelInstance.gameObject);
        }
    }
}
