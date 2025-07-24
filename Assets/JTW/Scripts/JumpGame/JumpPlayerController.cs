using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class JumpPlayerController : MonoBehaviourPun
{
    [SerializeField] private float jumpPower = 7f;
    private Rigidbody rigid;

    private bool isGround = true;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody>();
    }

    public void OnJump(InputValue value)
    {
        //if (!photonView.IsMine) return;
        if (!isGround) return;

        rigid.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
        isGround = false;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Finish"))
        {
            isGround = true;
        }
    }
}
