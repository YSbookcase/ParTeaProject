using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
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
        public void ShowDeathPanel(string winnerName)
        {
            if (deathPanel != null)
                deathPanel.SetActive(true);
        }

    }
}
