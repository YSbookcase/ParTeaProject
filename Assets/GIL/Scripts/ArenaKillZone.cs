using System.Collections;
using Photon.Pun;
using UnityEngine;

namespace GIL.Scripts
{
    public class ArenaKillZone : MonoBehaviour
    {
        private BoxCollider _collider;

        [SerializeField] private float shootSpeed = 100f;

        [SerializeField] private float shootTime = 1f;
        // Start is called before the first frame update
        private void Start()
        {
            _collider = GetComponent<BoxCollider>();
        }
        private void OnCollisionEnter(Collision other)
        {
            StartCoroutine(DestroyPlayer(other));
        }

        private IEnumerator DestroyPlayer(Collision other)
        {
            Rigidbody rb = other.collider.GetComponent<Rigidbody>();
            PhotonView view = other.gameObject.GetComponent<PhotonView>();
            
            if (rb != null)
            {
                rb.AddForce(Vector3.up * shootSpeed, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere.normalized, ForceMode.Impulse);
            }

            yield return new WaitForSeconds(shootTime);
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
