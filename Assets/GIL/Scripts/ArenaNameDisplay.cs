using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class ArenaNameDisplay : MonoBehaviourPun
{
    [SerializeField] private RectTransform namePanel;
    [SerializeField] private TextMeshProUGUI nameText;
    private Transform _mainCamera;

    private void Start()
    {
        if (photonView.Owner != null)
        {
            //nameText.text = photonView.Owner.NickName;
        }
        
        _mainCamera = Camera.main?.transform;
    }
    
    private void LateUpdate()
    {
        if (_mainCamera != null)
        {
            transform.forward = _mainCamera.forward;
        }
    }
}
