using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleHandler : MonoBehaviour
{
    private Rigidbody rigid;

    private Vector3 direction = Vector3.zero;
    private float speed;

    private void Start()
    {
        rigid = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        rigid.velocity = direction * speed;
    }

    public void Init(Vector3 direction, float speed)
    {
        this.direction = direction;
        this.speed = speed;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Respawn"))
        {
            Destroy(gameObject);
        }
    }
}
