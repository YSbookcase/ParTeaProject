using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PJW
{
    public class JumpRopeController : MonoBehaviour
    {
        [Header("회전 대상")]
        public Transform ropeTransform;

        [Header("회전 중심")]
        public Transform centerPoint;

        [Header("회전 설정")]
        public Vector3 rotationAxis = Vector3.up;
        public float initialSpeed = 30f;
        public float maxSpeed = 360f;
        public float acceleration = 10f;

        private float currentSpeed;

        void Start()
        {
            currentSpeed = initialSpeed;
        }

        void Update()
        {
            currentSpeed = Mathf.Min(currentSpeed + acceleration * Time.deltaTime, maxSpeed);

            // centerPoint기준으로 회전
            ropeTransform.RotateAround(centerPoint.position, rotationAxis, currentSpeed * Time.deltaTime);
        }
    }
}

