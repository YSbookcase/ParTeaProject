using System.Collections;
using Photon.Pun;
using UnityEngine;

namespace GIL.Scripts
{
    public class ArenaShrink : MonoBehaviourPun
    {
        private Transform _arenaTransform;
        [SerializeField] private float shrinkStartTime = 5f;
        [SerializeField] private float minShrinkScale = 1f;
        [SerializeField] private float shrinkDuration = 10f;
        
        private void Start()
        {
            _arenaTransform = GetComponent<Transform>();
            StartCoroutine(ShrinkArena());
        }
    
        private IEnumerator ShrinkArena()
        {
            yield return new WaitForSeconds(shrinkStartTime);
            Vector3 initialScale = _arenaTransform.localScale;
            Vector3 targetScale = new Vector3(minShrinkScale, initialScale.y, minShrinkScale);

            float elapsed = 0f;

            while (elapsed < shrinkDuration)
            {
                float t = elapsed / shrinkDuration;
                _arenaTransform.localScale = Vector3.Lerp(initialScale, targetScale, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _arenaTransform.localScale = targetScale;
        }
    }
}
