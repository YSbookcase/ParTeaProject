using System.Collections;
using Photon.Pun;
using UnityEngine;

namespace GIL.Scripts
{
    public class ArenaShrink : MonoBehaviourPun
    {
        [SerializeField] private Transform arenaTransform;
        [SerializeField] private float shrinkStartTime = 5f;
        [SerializeField] private float minShrinkScale = 1f;
        [SerializeField] private float shrinkDuration = 10f;
    
        private void Start()
        {
            StartCoroutine(ShrinkArena());
        }
    
        private IEnumerator TriggerShrinkRPC()
        {
            Debug.Log("Photon 호출 성공");
            yield return new WaitForSeconds(shrinkStartTime);
            Debug.Log("RPC 호출 시작");
            photonView.RPC(nameof(ArenaStartShrink), RpcTarget.All);
        }
    
        [PunRPC]
        private void ArenaStartShrink()
        {
            StartCoroutine(ShrinkArena());
        }
    
        private IEnumerator ShrinkArena()
        {
            yield return new WaitForSeconds(shrinkStartTime);
            Vector3 initialScale = arenaTransform.localScale;
            Vector3 targetScale = new Vector3(minShrinkScale, initialScale.y, minShrinkScale);

            float elapsed = 0f;

            while (elapsed < shrinkDuration)
            {
                float t = elapsed / shrinkDuration;
                arenaTransform.localScale = Vector3.Lerp(initialScale, targetScale, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            arenaTransform.localScale = targetScale;
        }
    }
}
