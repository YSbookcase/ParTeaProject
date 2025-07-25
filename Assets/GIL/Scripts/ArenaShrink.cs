using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArenaShrink : MonoBehaviour
{
    [SerializeField] private Transform arenaTransform;
    [SerializeField] private float shrinkStartTime = 5f;
    [SerializeField] private float shrinkFactor = 0.95f;
    [SerializeField] private float minShrinkScale = 1f;
    [SerializeField] private float shrinkDuration = 10f;
    
    private void Start()
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
