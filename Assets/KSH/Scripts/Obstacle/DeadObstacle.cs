using System.Collections;
using System.Collections.Generic;
using KSH;
using Photon.Pun;
using UnityEngine;

namespace KSH
{
    public class DeadObstacle : Obstacle
    {
        [SerializeField] private float respawnTime;
        [SerializeField] private GameObject DeadPanel;
        
        private PhotonView pv;
        
        void Start()
        {
            pv = GetComponent<PhotonView>();
        }
        protected override void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                PlayerController player = collision.gameObject.GetComponent<PlayerController>();
                if (player != null && PhotonView.Get(player).IsMine)
                {
                    StartCoroutine(Dead(player.gameObject));
                }
            }
        }

        private IEnumerator Dead(GameObject player)
        {
            pv.RPC("RPC_Dead", RpcTarget.AllBuffered, player.GetComponent<PhotonView>().ViewID, false);
            DeadPanel.SetActive(true);
            yield return new WaitForSeconds(respawnTime);
            
            Vector3 spawnPos = new Vector3(Random.Range(-1,5), 2, Random.Range(-1,5));
            player.transform.position = spawnPos;
            
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            pv.RPC("RPC_Dead", RpcTarget.AllBuffered, player.GetComponent<PhotonView>().ViewID, true);
            DeadPanel.SetActive(false);
        }

        [PunRPC]
        public void RPC_Dead(int id, bool isActive)
        {
            GameObject obj = PhotonView.Find(id).gameObject;
            obj.SetActive(isActive);
        }
        
    }    
}
