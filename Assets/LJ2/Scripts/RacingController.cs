using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using Photon.Realtime;

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
    private Quaternion networkRotation;

    private void Awake()
    {
        if (rigid == null)
        {
            rigid = GetComponent<Rigidbody>();
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 다른 플레이어에게 위치와 회전을 전송
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else if (stream.IsReading)
        {
            // 다른 플레이어로부터 위치와 회전을 수신
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
        }
    }

    private void Update()
    {
        if (photonView.IsMine)
        {
            Move();
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 10);
            transform.rotation = Quaternion.Lerp(transform.rotation,networkRotation,Time.deltaTime * 10);
        }
    }

    private void Move()
    {
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        Vector3 inputDirection = new Vector3(input.x, 0, input.y).normalized;
        Vector3 forward = transform.forward;

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
            moveDirection = forward;
        }

        rigid.velocity = moveDirection * currentSpeed;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position + transform.forward * 6, moveDirection * 10f);
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position + transform.forward * 6, transform.forward * 10f);
    }
}