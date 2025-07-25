using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PJW
{
    public class PlayerController : MonoBehaviourPun
    {
        [SerializeField] private float jumpForce;
        [SerializeField] private float bounceForce;

        [SerializeField] private float lerpSpeed = 20f; // 원격 플레이어 위치 보간 속도

        private Rigidbody playerRigidbody;
        private bool isGrounded;
        private bool isDead = false;

        private Vector3 networkPosition;
        private Quaternion networkRotation;

        private void Awake()
        {
            playerRigidbody = GetComponent<Rigidbody>();
            playerRigidbody.sleepThreshold = 0f; // 가만히 있는 상태에서도 리지드 바디를 적용시킴

            if (!photonView.IsMine)
            {
                playerRigidbody.isKinematic = true;
            }
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

                if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
                    Jump();
            }
            else
            {
                // 원격 플레이어 보간 적용
                transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 10f);
                transform.rotation = Quaternion.Slerp(transform.rotation, networkRotation, Time.deltaTime * 10f);
            }
        }

        private void Jump()
        {
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

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);
            }
            else
            {
                networkPosition = (Vector3)stream.ReceiveNext();
                networkRotation = (Quaternion)stream.ReceiveNext();
            }
        }
    }
}
