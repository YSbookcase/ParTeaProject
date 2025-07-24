using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace KSH
{
    public class PlayerController : MonoBehaviourPun
    {
        [Header("움직임 관련")] [SerializeField] private float moveSpeed;
        [SerializeField] private float rotateSpeed;
        [Header("테스트용 (PC도 조작됨)")] public bool enableMoblie = false;
        [SerializeField] private Material body;
        [SerializeField] private Renderer bodyRenderer;

        private Rigidbody rigid;
        private Vector3 moveVec;
        public Color color;
        private VariableJoystick joystick;

        void Start()
        {
            rigid = GetComponent<Rigidbody>();
            joystick = FindObjectOfType<VariableJoystick>();
            
            ChangeColor();
        }

        void FixedUpdate()
        {
            if (photonView.IsMine)
            {
                Move();
            }
        }

        private void Move()
        {
            if (enableMoblie) //true면 모바일 조이스틱 사용
            {
                float x = joystick.Horizontal;
                float z = joystick.Vertical;
                moveVec = new Vector3(x, 0, z) * moveSpeed * Time.deltaTime; //초당 이동속도만큼 이동하는 벡터
            }
            else
            {
                float x = Input.GetAxis("Horizontal");
                float z = Input.GetAxis("Vertical");
                moveVec = new Vector3(x, 0, z) * moveSpeed * Time.deltaTime;
            }

            rigid.MovePosition(transform.position + moveVec); //현재위치에서 이동벡터만큼 이동

            if (moveVec.sqrMagnitude == 0) return; //만약 움직임이 없다면 반환

            Quaternion dirQuat = Quaternion.LookRotation(moveVec); //이동벡터를 바라보며 회전
            //현재 회전에서 목표회전까지 회전 보간하고 회전 속도에 따라 시간 기반으로 조절하게 함
            Quaternion moveQuat = Quaternion.Slerp(rigid.rotation, dirQuat, rotateSpeed * Time.deltaTime);
            rigid.MoveRotation(moveQuat); //설정한 회전값으로 회전
        }
        
        public void SettingColor(Color newcolor) //색깔 세팅
        {
            color = newcolor;
            if (bodyRenderer != null)
            {
                bodyRenderer.material.color = color;
            }
        }

        public void ChangeColor()
        {
            body = new Material(body); // 새 메터리얼 생성
        
            if(bodyRenderer != null)
                bodyRenderer.material = body;
        
            ColorManager.Instance.RegisterPlayer(this); //매니저에 플레이어를 등록
        
            //만약 포톤뷰를 소유한 플레이어의 커스텀프로퍼티에서 Color키를 찾으면
            if (photonView.Owner.CustomProperties.TryGetValue("Color", out object value))
            {
                //value가 문자열이고 Color타입으로 변환할 수 있다면
                if (value is string colorHex && ColorUtility.TryParseHtmlString("#" + colorHex, out color))
                {
                    SettingColor(color);
                }
            }
        }
    }
}