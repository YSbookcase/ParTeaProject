using Photon.Pun;
using UnityEngine;

namespace GIL.Scripts
{
    public class ArenaKillZone : MonoBehaviour
    {
        private BoxCollider _collider;

        [SerializeField] private float shootSpeed = 100f;
        // Start is called before the first frame update
        private void Start()
        {
            _collider = GetComponent<BoxCollider>();
        }
        private void OnCollisionEnter(Collision other)
        {
            Rigidbody rb = other.collider.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(Vector3.up * shootSpeed, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere.normalized, ForceMode.Impulse);
            }

            PhotonView view = other.gameObject.GetComponent<PhotonView>();
            if (view == null) return;

            if (view.IsMine)
            {
                // 네트워크 전체에서 플레이어 오브젝트 삭제
                PhotonNetwork.Destroy(view.gameObject);

                // 마스터에게 사망 정보 전달
                PhotonView managerView = ArenaGameManager.Instance.photonView;
                managerView.RPC(nameof(ArenaGameManager.ArenaPlayerDied), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
            }
        }
    }
}
