using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ArenaKillZone : MonoBehaviour
{
    private BoxCollider _collider;

    [SerializeField] private float shootSpeed = 100f;
    // Start is called before the first frame update
    private void Start()
    {
        _collider = GetComponent<BoxCollider>();
    }
    private void OnCollisionEnter(Collision other)
    {
        Rigidbody rb = other.collider.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(Vector3.up * shootSpeed, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere.normalized, ForceMode.Impulse);
        }

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonView view = other.gameObject.GetComponent<PhotonView>();
            if (view != null && view.Owner != null)
            {
                ArenaGameManager.Instance?.PlayerDied(view.Owner);
            }
            else
            {
                Debug.LogWarning("PhotonView 또는 Owner가 없음 → PlayerDied 호출 안 함");
            }
        }

        Destroy(other.gameObject, 2f);
    }
}
