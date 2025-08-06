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
        [SerializeField] private GameObject effectPrefab;
        [SerializeField] private string  soundEffectName;
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
                
                PhotonView otherPhotonView = other.gameObject.GetComponent<PhotonView>();
                if (otherPhotonView != null)
                {
                    Vector3 hitPos = other.contacts[0].point;

                    if (PhotonNetwork.IsConnected == false)
                    {
                        Debug.Log("이펙트 발생");
                        Instantiate(effectPrefab, hitPos, Quaternion.identity);
                        Manager.Audio.SfxPlay(soundEffectName);
                    }
                    
                    otherPhotonView.RPC(nameof(ArenaKillzoneEffect), RpcTarget.All, hitPos);
                }
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
        
        [PunRPC]
        public void ArenaKillzoneEffect(Vector3 pos)
        {
            Instantiate(effectPrefab, pos, Quaternion.identity);
            Manager.Audio.SfxPlay(soundEffectName);
        }
    }
}
