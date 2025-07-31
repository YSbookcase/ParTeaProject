using Photon.Pun;
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
        PhotonView photonView = other.gameObject.GetComponent<PhotonView>();
        if (controller != null && photonView.IsMine) 
        {
            if (controller.linePassed < havePassLine) 
            {
                controller.linePassed++;
            }
            else
            {
                Debug.Log("플레이어 도착");

                if (photonView != null)
                {
                    controller.isControllable = false;
                    RacingManager.Instance.managerView.RPC("PlayerArrive", RpcTarget.MasterClient, photonView.Owner.ActorNumber);
                }
            }
        }
        
    }
}
