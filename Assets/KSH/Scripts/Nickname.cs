using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Nickname : MonoBehaviour
{
    private Transform camera;

    private void Start()
    {
        camera = Camera.main.transform;
    }

    private void Update()
    {
        transform.LookAt(transform.position + camera.forward);
    }
}
