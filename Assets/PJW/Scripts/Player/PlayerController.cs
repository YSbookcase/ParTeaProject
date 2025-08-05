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
        [SerializeField] private Animator animator;

        private Rigidbody playerRigidbody;
        private bool isGrounded;
        private bool isDead = false;
        private bool hasJumped = false;

        public bool HasJumped => hasJumped;

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

        [PunRPC]
        public void RPCAddRopePassScore()
        {
            if (photonView.IsMine && !isDead && hasJumped)
            {
                PhotonNetwork.LocalPlayer.AddRopeGameScore(1);
                hasJumped = false;
            }
        }

        private void Jump()
        {
            if (!isGrounded || isDead || !photonView.IsMine) return;

            playerRigidbody.velocity = new Vector3(
                playerRigidbody.velocity.x,
                jumpForce,
                playerRigidbody.velocity.z
            );
            isGrounded = false;
            hasJumped = true;

            photonView.RPC(nameof(RPCRopeSetJumping), RpcTarget.All, true);
            AudioManager.Instance.SfxPlay("JumpSound", transform);
        }

        [PunRPC]
        private void RPCRopeSetJumping(bool isJumping)
        {
            animator.SetBool("IsJumping", isJumping);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!photonView.IsMine) return;

            if (collision.gameObject.CompareTag("Ground"))
            {
                isGrounded = true;
                photonView.RPC(nameof(RPCRopeSetJumping), RpcTarget.All, false);
                hasJumped = false;
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

            photonView.RPC(
                 nameof(RPCRopeNotifyDeath),
                 RpcTarget.MasterClient,
                 PhotonNetwork.LocalPlayer.ActorNumber
             );

            AudioManager.Instance.SfxPlay("Die", transform);
        }

        [PunRPC]
        private void RPCRopeNotifyDeath(int actorNumber, PhotonMessageInfo info)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            RopeGameManager manager = FindObjectOfType<RopeGameManager>();
            if (manager == null)
                return;

            Player targetPlayer = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
            if (targetPlayer != null)
            {
                manager.OnPlayerDied(targetPlayer);
            }
        }
    }
}
