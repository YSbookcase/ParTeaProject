using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using Photon.Realtime;
using Cinemachine;

public class RacingController : MonoBehaviourPun, IPunObservable
{
    [SerializeField] float maxSpeed;
    [SerializeField] float acceleration;
    [SerializeField] float turnSpeed;
    [SerializeField] private float driftTurnSpeed;

    [SerializeField] Rigidbody rigid;

    [SerializeField] private InputActionReference moveAction;
    private Vector3 moveDirection;
    private float currentSpeed;

    private Vector3 networkPosition;
    private Vector3 networkVelocity;
    private Quaternion networkRotation;

    private CinemachineVirtualCamera virtualCamera;

    private void Awake()
    {
        if (rigid == null)
        {
            rigid = GetComponent<Rigidbody>();
        }
    }

    private void Start()
    {
        if (photonView.IsMine)
        {
            virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
            if (virtualCamera != null)
            {
                virtualCamera.Follow = transform;
                virtualCamera.LookAt = transform;
            }
        }
    }

    private void OnEnable()
    {
        moveAction.action.Enable();
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
        if (photonView.IsMine)
        {
            SetRotation();
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


    private void SetRotation()
    {
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        Vector3 inputDirection = new Vector3(input.x, 0, input.y).normalized;
       

        // 차량 방향 전환
        if (inputDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
        }

        // 차량 속도 조절
        float targetSpeed = (inputDirection != Vector3.zero) ? maxSpeed : 0f;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);

        // 차량이동 방향 전환
        if (inputDirection != Vector3.zero)
        {
            moveDirection = Vector3.Slerp(moveDirection, inputDirection, Time.deltaTime * driftTurnSpeed);
        }
        else
        {
            moveDirection = transform.forward;
        }
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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, moveDirection * 10f);
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.forward * 10f);
    }
}