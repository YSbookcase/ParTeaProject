using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RacingController : MonoBehaviour
{
    [SerializeField] float moveSpeed;
    [SerializeField] float turnSpeed;
    [SerializeField] Rigidbody rigid;

    [SerializeField] private InputActionReference moveAction;
    private Vector3 moveDirection;

    private void Awake()
    {
        if (rigid == null)
        {
            rigid = GetComponent<Rigidbody>();
        }
    }

    private void Update()
    {
        Move();
    }

    private void Move()
    {
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        Vector3 inputDirection = new Vector3(input.x, 0, input.y).normalized;
        Vector3 forward = transform.forward;

        float driftTurnSpeed = turnSpeed * 0.1f; // 차량 회전 보다 느리게 
        if (inputDirection != Vector3.zero)
        {
            moveDirection = Vector3.Slerp(moveDirection, inputDirection, Time.deltaTime * driftTurnSpeed).normalized;
        }
        else
        {
            moveDirection = forward;
        }

        rigid.velocity = moveDirection * moveSpeed;

        if(inputDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
        }

    }


}
