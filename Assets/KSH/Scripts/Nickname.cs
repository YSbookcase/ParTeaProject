using UnityEngine;

namespace KSH
{
    public class Nickname : MonoBehaviour
    {
        private Transform myCamera;

        private void Start()
        {
            myCamera = Camera.main.transform;
        }

        private void Update()
        {
            transform.LookAt(transform.position + myCamera.forward);
        }
    }    
}
