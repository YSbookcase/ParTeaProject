using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PJW
{
    public class PlayerController : MonoBehaviourPun
    {
        [SerializeField] private float jumpForce;
        [SerializeField] private float bounceForce;


        private Rigidbody playerRigidbody;
        private bool isGrounded;
        private bool isDead = false;

        private Vector3 networkPosition;
        private Quaternion networkRotation;

        private PlayerInputActions inputActions;

        private void Awake()
        {
            playerRigidbody = GetComponent<Rigidbody>();
            playerRigidbody.sleepThreshold = 0f; // 가만히 있는 상태에서도 리지드 바디를 적용시킴

            if (!photonView.IsMine)
            {
                playerRigidbody.isKinematic = true;
            }

            inputActions = new PlayerInputActions();
            inputActions.Player_PJW.Jump.performed += ctx => Jump();
        }

        private void OnEnable()
        {
            inputActions.Enable();
        }

        private void OnDisable()
        {
            inputActions.Disable();
        }

        private void Start()
        {
            networkPosition = transform.position;
            networkRotation = transform.rotation;
        }

        private void Update()
        {
            if (photonView.IsMine)
            {
                if (isDead) return;
            }
            /*else
            {
                // 원격 플레이어 보간 적용
                // transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 10f);
                float x = Mathf.Lerp(transform.position.x, networkPosition.x, Time.deltaTime);
                float z = Mathf.Lerp(transform.position.z, networkPosition.z, Time.deltaTime);
                transform.position = new Vector3(x, networkPosition.y, z);
                transform.rotation = Quaternion.Slerp(transform.rotation, networkRotation, Time.deltaTime * 10f);
            }*/
        }

        private void Jump()
        {
            if (!isGrounded || isDead || !photonView.IsMine) return;

            Vector3 velocity = playerRigidbody.velocity;
            velocity.y = jumpForce;
            playerRigidbody.velocity = velocity;
            isGrounded = false; 
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!photonView.IsMine)
                return;

            if (collision.gameObject.CompareTag("Ground"))
            {
                isGrounded = true;
            }
            else if (!isDead && collision.gameObject.CompareTag("Rope"))
            {
                BounceDie();
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (!photonView.IsMine)
                return;

            if (collision.gameObject.CompareTag("Ground"))
            {
                isGrounded = false;
            }
        }

        private void BounceDie()
        {
            if (isDead) return;
            isDead = true;

            Vector3 bounceDir = (Vector3.forward + Random.onUnitSphere).normalized;
            playerRigidbody.AddForce(bounceDir * bounceForce, ForceMode.Impulse); // 로프에 닿으면 날아감
        }

        // public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        // {
        //     if (stream.IsWriting)
        //     {
        //         stream.SendNext(transform.position);
        //         stream.SendNext(transform.rotation);
        //     }
        //     else
        //     {
        //         networkPosition = (Vector3)stream.ReceiveNext();
        //         networkRotation = (Quaternion)stream.ReceiveNext();
        //     }
        // }
    }
}
