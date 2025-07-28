using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RacingLine : MonoBehaviour
{
    [SerializeField] public List<Transform> spawnPositions = new List<Transform>();
    
    [SerializeField] public float pathPosition;
    [SerializeField] public GameObject bottomLine;
    [SerializeField] public GameObject goalQuad;

    public int havePassLine;

    private void Awake()
    {
        bottomLine.SetActive(false);
        goalQuad.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        RacingController controller = other.GetComponent<RacingController>();
        if (controller != null) 
        {
            if (controller.linePassed < havePassLine) 
            {
                controller.linePassed++;
            }
            else
            {
                Debug.Log("플레이어 도착");
            }
        }
        
    }
}
