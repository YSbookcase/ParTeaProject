using UnityEngine;
using Photon.Pun;
using TMPro;
using UnityEngine.UI;

namespace KSH
{
    public class PlayerController : MonoBehaviourPun,IPunObservable
    {
        [Header("움직임 관련")] 
        [SerializeField] private float moveSpeed;
        [SerializeField] private float rotateSpeed;
        public bool isMove = true;
        [Header("색깔 관련")]
        [SerializeField] private Material body;
        [SerializeField] private SkinnedMeshRenderer bodyRenderer;
        public Color color;
        [SerializeField] private TextMeshProUGUI nickName;
        public TextMeshProUGUI NickName => nickName;
        
        [SerializeField] private Texture2D[] textures;

        private Animator animator;
        private Rigidbody rigid;
        private Vector3 moveVec;
        private Vector2 inputDir;
        private PlayerAction playerAction;
        private float curSpeed;
        private Vector3 photonPosition; //보간
        private Quaternion photonRotation; //보간
        private Vector3 previousPhotonPosition;
        private double lastPacketTime;
        
        private void Awake()
        {
            playerAction = new PlayerAction();
            rigid = GetComponent<Rigidbody>();
            isMove = true;
            curSpeed = moveSpeed;
            
            if (photonView.IsMine)
            {
                playerAction = new PlayerAction();
            }
        }
        
        private void OnEnable()
        {
            if (photonView.IsMine && playerAction != null)
                playerAction.Enable();
        }

        private void OnDisable()
        {
            if (photonView.IsMine && playerAction != null)
                playerAction.Disable();
        }

        private void Start()
        {
            animator = GetComponent<Animator>();
            
            if (photonView.IsMine)
            {
                nickName.text = PhotonNetwork.NickName;
                
                FindObjectOfType<FollowCamera>().SetCameraTarget(this.transform);
            }
            else
            {
                nickName.text = photonView.Owner.NickName;
            }
            ColorManager.Instance.RegisterPlayer(this); //매니저에 플레이어를 등록
            ChangeColor();
            PlayerColor();
        }

        void Update()
        {
            inputDir = playerAction.Player.Move.ReadValue<Vector2>();

            if (photonView.IsMine)
            {
                float currentSpeed = inputDir.magnitude;
                animator.SetFloat("Speed", currentSpeed);
            }
            else
            {
                Vector3 velocity = (photonPosition - previousPhotonPosition) / Time.deltaTime;
                float lag = (float)(PhotonNetwork.Time - lastPacketTime);

                Vector3 predictedPosition = photonPosition + velocity * lag;

                transform.position = Vector3.Lerp(transform.position, predictedPosition, Time.deltaTime * 20f);
                transform.rotation = Quaternion.Lerp(transform.rotation, photonRotation, Time.deltaTime * 20f);
            }
        }

        void FixedUpdate()
        {
            if(photonView.IsMine && isMove)
                Move();
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);
            }
            else if (stream.IsReading)
            {
                previousPhotonPosition = photonPosition;
                photonPosition = (Vector3)stream.ReceiveNext();
                photonRotation = (Quaternion)stream.ReceiveNext();
                lastPacketTime = info.SentServerTime;
            }
        }
        
        private void DontMove()
        {
            moveVec = Vector3.zero;
            isMove = false;
        }
        public void CanMove(Vector3 moveVec)
        {
            this.moveVec = moveVec;
            isMove = true;
        }

        [PunRPC]
        public void RPC_CanMove()
        {
            CanMove(moveVec);
        }
        
        [PunRPC]
        public void RPC_DontMove()
        {
            DontMove();
        }
        
        public void Move()
        {
            moveVec = new Vector3(inputDir.x, 0, inputDir.y) * curSpeed * Time.fixedDeltaTime; //초당 이동속도만큼 이동하는 벡터

            rigid.MovePosition(transform.position + moveVec); //현재위치에서 이동벡터만큼 이동

            if (moveVec.sqrMagnitude == 0) return; //만약 움직임이 없다면 반환

            Quaternion dirQuat = Quaternion.LookRotation(moveVec); //이동벡터를 바라보며 회전
            //현재 회전에서 목표회전까지 회전 보간하고 회전 속도에 따라 시간 기반으로 조절하게 함
            Quaternion moveQuat = Quaternion.Slerp(rigid.rotation, dirQuat, rotateSpeed * Time.deltaTime);
            if (photonView.IsMine)
            {
                rigid.MoveRotation(moveQuat); //설정한 회전값으로 회전
            } 
        }
        
        public void SettingColor(Color newcolor) //색깔 세팅
        {
            color = newcolor;
            if(nickName != null)
            {
                nickName.color = color;
            }
        }

        public void ChangeColor()
        {
            if (photonView.Owner.CustomProperties.TryGetValue("TeamColor", out object value))
            {
                if (value is string colorHex)
                {
                    if (ColorUtility.TryParseHtmlString("#" + colorHex, out color))
                    {
                        SettingColor(color);
                    }
                }
            }
        }

        public void PlayerColor()
        {
            if(photonView.Owner.CustomProperties.TryGetValue("Color", out object value))
            {
                int colorIndex = (int)value;

                if (colorIndex >= 0 && colorIndex < textures.Length)
                {
                    bodyRenderer.material.mainTexture = textures[colorIndex];
                }
            }
        }
        
        public void Bounce(float bounceForce)
        {
            Vector3 velocity = rigid.velocity;
            velocity.y = 0;
            rigid.velocity = velocity;
            rigid.AddForce(transform.up * bounceForce, ForceMode.Impulse);
        }

        public void Slow(float slowFactor)
        {
            curSpeed = moveSpeed * slowFactor;
        }

        public void ResetSpeed()
        {
            curSpeed = moveSpeed;
        }
    }
}