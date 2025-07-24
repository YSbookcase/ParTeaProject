using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PJW
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float jumpForce = 5f;
        [SerializeField] private float bounceForce = 25f;

        private Rigidbody playerRigidbody;
        private bool isGrounded;
        private bool isDead = false;

        private void Awake()
        {
            playerRigidbody = GetComponent<Rigidbody>();
            playerRigidbody.sleepThreshold = 0f; // 가만히 있는 상태에서도 리지드 바디를 적용시킴
        }

        private void Update()
        {
            if (isDead) return; // if (!photonView.IsMine || isDead) return;

            if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            {
                Jump();
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
            if (collision.gameObject.CompareTag("Ground"))
            {
                isGrounded = true;
            }
            else if (!isDead && collision.gameObject.CompareTag("Rope"))
            {
                Die();
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (collision.gameObject.CompareTag("Ground"))
            {
                isGrounded = false;
            }
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            Vector3 bounceDir = (Vector3.forward + Random.onUnitSphere).normalized;
            playerRigidbody.AddForce(bounceDir * bounceForce, ForceMode.Impulse); // 로프에 닿으면 날아감
        }
    }
}
