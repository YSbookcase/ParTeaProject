using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.InputSystem;

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
        private Vector2 inputDir;
        private PlayerAction playerAction;

        private void Awake()
        {
            playerAction = new PlayerAction();
            rigid = GetComponent<Rigidbody>();
        }
        
        private void OnEnable()
        {
            playerAction.Enable();
        }

        private void OnDisable()
        {
            playerAction.Disable();
        }

        void Start()
        {
            ChangeColor();
        }

        void Update()
        {
            inputDir = playerAction.Player.Move.ReadValue<Vector2>();
        }

        void FixedUpdate()
        {
            if(photonView.IsMine)
                Move();
        }
        

        public void Move()
        {
            moveVec = new Vector3(inputDir.x, 0, inputDir.y) * moveSpeed * Time.fixedDeltaTime; //초당 이동속도만큼 이동하는 벡터

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