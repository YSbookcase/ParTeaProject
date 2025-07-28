using UnityEngine;
using Photon.Pun;

namespace PJW
{
    public class JumpRopeController : MonoBehaviourPun, IPunObservable
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

        // 동기화용 값
        private Vector3 networkPosition;
        private Quaternion networkRotation;

        private void Start()
        {
            currentSpeed = initialSpeed;
            if (!PhotonNetwork.IsMasterClient)
            {
                networkPosition = ropeTransform.position;
                networkRotation = ropeTransform.rotation;
            }
        }

        private void Update()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                currentSpeed = Mathf.Min(currentSpeed + acceleration * Time.deltaTime, maxSpeed);
                ropeTransform.RotateAround(centerPoint.position, rotationAxis, currentSpeed * Time.deltaTime);
            }
            else
            {
                // 위치와 회전을 함께 보간
                ropeTransform.position = Vector3.Lerp(ropeTransform.position, networkPosition, Time.deltaTime * 10f);
                ropeTransform.rotation = Quaternion.Lerp(ropeTransform.rotation, networkRotation, Time.deltaTime * 10f);
            }
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(ropeTransform.position);
                stream.SendNext(ropeTransform.rotation);
            }
            else
            {
                networkPosition = (Vector3)stream.ReceiveNext();
                networkRotation = (Quaternion)stream.ReceiveNext();
            }
        }
    }
}
