using UnityEngine;
using Photon.Pun;

namespace PJW
{
    [RequireComponent(typeof(PhotonView))]
    public class JumpRopeController : MonoBehaviourPun
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

        private bool hasStarted = false;
        private double ropeStartTime;

        private float cumulativeAngle = 0f;

        [PunRPC]
        public void RPCStartRope(double startTimestamp)
        {
            ropeStartTime = startTimestamp;
            hasStarted = true;
        }

        private void Update()
        {
            if (!hasStarted) return;

            double elapsed = PhotonNetwork.Time - ropeStartTime;
            if (elapsed < 0) return;

            float currentSpeed = Mathf.Min(initialSpeed + acceleration * (float)elapsed, maxSpeed);
            float deltaAngle = currentSpeed * Time.deltaTime;
            ropeTransform.RotateAround(centerPoint.position, rotationAxis, deltaAngle);

            if (PhotonNetwork.IsMasterClient)
            {
                cumulativeAngle += deltaAngle;
                if (cumulativeAngle >= 360f)
                {
                    cumulativeAngle -= 360f;
                    DistributePassScore();
                }
            }
        }

        // 한 바퀴 돌 때마다 모든 PlayerController에 RPC 호출
        private void DistributePassScore()
        {
            foreach (var pc in FindObjectsOfType<PlayerController>())
            {
                if (pc.photonView != null)
                {
                    pc.photonView.RPC(
                        nameof(PlayerController.RPCAddRopePassScore),
                        pc.photonView.Owner  // 해당 플레이어의 클라이언트로만 RPC
                    );
                }
            }
        }
    }
}
