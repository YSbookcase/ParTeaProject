using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PJW
{
    public class DeathPanelManager : MonoBehaviour
    {
        [SerializeField] private GameObject deathPanel;

        public void ShowDeathPanel()
        {
            deathPanel.SetActive(true);
        }

        public void HideDeathPanel()
        {
            deathPanel.SetActive(false);
        }
    }
}
