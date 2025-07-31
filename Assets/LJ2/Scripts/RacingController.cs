using Cinemachine;
using Photon.Pun;
using Photon.Pun.Demo.PunBasics;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RacingController : MonoBehaviourPun, IPunObservable
{
    [SerializeField] float maxSpeed;
    [SerializeField] float acceleration;
    [SerializeField] float turnSpeed;
    [SerializeField] private float driftTurnSpeed;

    [SerializeField] private Image arrow;

    [SerializeField] Rigidbody rigid;

    [SerializeField] private InputActionReference moveAction;
    private Vector3 moveDirection;
    private float currentSpeed;

    private Vector3 networkPosition;
    private Vector3 networkVelocity;
    private Quaternion networkRotation;

    private CinemachineVirtualCamera virtualCamera;
    private CinemachineDollyCart dollyCart;
    private Vector3 previousPosition;

    [SerializeField] public float cameraSpeed;

    public int linePassed;
    public bool isControllable;

    private void Awake()
    {
        if (rigid == null)
        {
            rigid = GetComponent<Rigidbody>();
        }

        dollyCart = FindObjectOfType<CinemachineDollyCart>();
    }

    private void Start()
    {
        if (photonView.IsMine)
        {
            virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
            if (virtualCamera != null)
            {
                //virtualCamera.Follow = transform;
                virtualCamera.LookAt = transform;
            }
            previousPosition = transform.position;
            linePassed = 0;
        }
        else 
        {
            arrow.enabled = false; // 다른 플레이어의 화살표 비활성화
        }
    }

    private void OnEnable()
    {
        moveAction.action.Enable();
        if (photonView.IsMine)
        {
            RacingManager.Instance.racingControllers[PhotonNetwork.LocalPlayer.ActorNumber] = this;
        }
    }
    private void OnDisable()
    {
        moveAction.action.Disable();
    }
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 다른 플레이어에게 위치와 회전을 전송
            stream.SendNext(rigid.position);
            stream.SendNext(rigid.velocity);
            stream.SendNext(rigid.rotation);
        }
        else if (stream.IsReading)
        {
            // 다른 플레이어로부터 위치와 회전을 수신
            networkPosition = (Vector3)stream.ReceiveNext();
            networkVelocity = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
        }
    }

    private void Update()
    {
        if (photonView.IsMine && isControllable)
        {
            //SetRotation();
            SetRotationByCam();
            DollyCartMove();
        }
        
    }

    private void FixedUpdate()
    {
        if (photonView.IsMine)
        {
            rigid.velocity = moveDirection * currentSpeed;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 10);
            rigid.velocity = Vector3.Lerp(rigid.velocity, networkVelocity, Time.deltaTime * 10);
            transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * 10);
        }
    }

    [PunRPC]
    public void SetControllable(bool controllable)
    {
        isControllable = controllable;
        if (controllable)
        {
            moveDirection = transform.forward; // 초기 방향 설정
            currentSpeed = 0f; // 초기 속도 설정
        }
        else
        {
            moveDirection = Vector3.zero; // 컨트롤 불가능 시 방향 초기화
            currentSpeed = 0f; // 컨트롤 불가능 시 속도 초기화
        }
    }


    private void SetRotationByCam()
    {
        Vector2 input = moveAction.action.ReadValue<Vector2>();

        if (virtualCamera != null)
        {
            Transform camTransform = virtualCamera.transform;

            // 카메라 기준 방향 변환
            Vector3 camForward = camTransform.forward;
            Vector3 camRight = camTransform.right;

            // y축 제외한 평면 방향 만들기
            camForward.y = 0f;
            camRight.y = 0f;

            camForward.Normalize();
            camRight.Normalize();

            // 카메라 기준으로 입력 방향 구성
            Vector3 inputDirection = (camForward * input.y + camRight * input.x).normalized;

            float directionDot = Vector3.Dot(inputDirection, dollyCart.transform.forward);
            if (directionDot < -0.5f) // 방향이 너무 반대에 가까우면
            {
                inputDirection = Vector3.zero; // 입력 무시
            }

            // 차량 회전 처리
            if (inputDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
            }

            float targetSpeed = (inputDirection != Vector3.zero) ? maxSpeed : 0f;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);

            if (inputDirection != Vector3.zero)
            {
                moveDirection = Vector3.Slerp(moveDirection, inputDirection, Time.deltaTime * driftTurnSpeed);
            }
            else
            {
                moveDirection = transform.forward;
            }

        }
    }

    private void DollyCartMove()
    {
        if (dollyCart == null) return;

        Vector3 displacement = transform.position - previousPosition;
        float moveDistance = displacement.magnitude;

        if (moveDistance < 0.1f) return; // 거의 정지 상태면 무시

        Vector3 moveDir = displacement.normalized;
        float alignment = Vector3.Dot(moveDir, dollyCart.transform.forward);
        float adjustedSpeed = moveDistance * Mathf.Clamp01(alignment) / Time.deltaTime;

        dollyCart.m_Position += adjustedSpeed * cameraSpeed * Time.deltaTime;
        previousPosition = transform.position;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(!photonView.IsMine) return;

        float impactForce = collision.relativeVelocity.magnitude;

        if (impactForce < 5f)
        {
            rigid.velocity *= 0.8f; // 약한 충돌은 속도 감소
        }
        else
        {
            // 중간 충돌은 약간의 반동
            rigid.AddForce(-collision.relativeVelocity.normalized * impactForce * 0.5f, ForceMode.Impulse);
        }

        Vector3 pushDirection = (transform.position - collision.transform.position).normalized;
        float strength = Mathf.Clamp(impactForce * 0.5f , 5f, 20f); // 충돌 강도에 따라 힘 조절

        if (collision.gameObject.CompareTag("Player"))
        {
            PhotonView targetView = collision.gameObject.GetComponent<PhotonView>();
            if (targetView != null && targetView.IsMine == false)
            {
                photonView.RPC("RacingCrash", RpcTarget.All, pushDirection * strength, targetView.ViewID);
            }
        }
    }


    private void OnCollisionExit(Collision collision)
    {
        if(!photonView.IsMine) return;
        rigid.velocity *= 0.9f; // 충돌 후 속도 감소
        rigid.angularVelocity = Vector3.zero; // 회전 속도 초기화
    }

    [PunRPC]
    public void RacingCrash(Vector3 direction, int targetViewID)
    {
        if(photonView.ViewID != targetViewID) return; // 자신의 뷰 ID가 아니면 무시

        rigid.AddForce(direction, ForceMode.Impulse);
        rigid.angularVelocity = Vector3.zero; // 회전 속도 초기화
    }

}