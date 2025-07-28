using Photon.Pun;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Realtime;

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
            playerRigidbody.sleepThreshold = 0f;

            if (!photonView.IsMine)
            {
                playerRigidbody.isKinematic = true;
            }

            inputActions = new PlayerInputActions();
            inputActions.Player_PJW.Jump.performed += ctx => Jump();
        }

        private void OnEnable() => inputActions.Enable();
        private void OnDisable() => inputActions.Disable();

        private void Start()
        {
            networkPosition = transform.position;
            networkRotation = transform.rotation;
        }

        private void Update()
        {
            if (photonView.IsMine && isDead) return;
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
            if (!photonView.IsMine) return;

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
            if (!photonView.IsMine) return;

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
            playerRigidbody.AddForce(bounceDir * bounceForce, ForceMode.Impulse);

            // 마스터 클라이언트에게 사망 통지
            photonView.RPC(nameof(RPC_NotifyDeath), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
        }

        [PunRPC]
        private void RPC_NotifyDeath(int actorNumber, PhotonMessageInfo info)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            RopeGameManager manager = FindObjectOfType<RopeGameManager>();
            if (manager != null)
            {
                Player targetPlayer = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
                manager.OnPlayerDied_RPC(targetPlayer);
            }
        }
    }
}
