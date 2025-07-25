using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArenaKillZone : MonoBehaviour
{
    [SerializeField] private BoxCollider collider;

    [SerializeField] private float shootSpeed = 100f;
    // Start is called before the first frame update

    private void OnCollisionEnter(Collision other)
    {
        Rigidbody rb = other.collider.GetComponent<Rigidbody>();
        //rb.useGravity = false;
        rb.AddForce(Vector3.up * shootSpeed, ForceMode.Impulse);
        Vector3 rotateVector = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ).normalized;
        rb.AddTorque(rotateVector,  ForceMode.Impulse);
        
        Destroy(other.gameObject, 2f);
    }
}
