using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PJW
{
    public class RopeUIManager : MonoBehaviourPun
    {
        public static RopeUIManager Instance { get; private set; }
        
        [SerializeField] private GameObject deathPanel;
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (deathPanel != null)
            {
                deathPanel.SetActive(false);
            }
        }

        // [PunRPC]
        public void ShowDeathPanel()
        {
            if (deathPanel != null)
            {
                deathPanel.SetActive(true);
            }
        }

       // public void ShowDeathPanel() => RPCShowDeathPanel();
    }
}
