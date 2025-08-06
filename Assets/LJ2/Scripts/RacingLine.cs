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
    public bool isGoalLine;

    private void Awake()
    {
        bottomLine.SetActive(false);
        goalQuad.SetActive(false);
        isGoalLine = false;
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
                if (photonView != null && isGoalLine)
                {
                    RacingManager.Instance.managerView.RPC("PlayerArrive", RpcTarget.MasterClient, photonView.Owner.ActorNumber);
                }
            }
        }
        
    }

    private void OnTriggerExit(Collider other)
    {
        RacingController controller = other.GetComponent<RacingController>();
        PhotonView photonView = other.gameObject.GetComponent<PhotonView>();
        if (controller != null && photonView.IsMine) 
        {
            if(controller.linePassed > havePassLine && photonView != null && isGoalLine) 
            {
                controller.SetControllable(false);
                controller.StopRacingSound();
            }
        }
    }
}
