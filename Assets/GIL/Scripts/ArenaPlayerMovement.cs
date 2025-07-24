using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
namespace GIL.Scripts
{
    public class ArenaPlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float movePower = 50f;
        [SerializeField] private float maxSpeed = 15f;
        [SerializeField] private float drag = 0.9f;
        
        private ArenaPlayerActions _inputActions;
        private Rigidbody _rigidbody;
        private Camera _mainCamera;
        private PhotonView _photonView;

        private Vector2 _startTouchPos;
        private bool _isTouching = false;
        
        private void Awake()
        {
            _inputActions = new ArenaPlayerActions();
            _rigidbody = GetComponent<Rigidbody>();
            _mainCamera = Camera.main;
            _photonView = GetComponent<PhotonView>();
        }

        private void OnEnable()
        {
            if (_photonView.IsMine) _inputActions.Enable();
        }

        private void OnDisable()
        {
            if (_photonView.IsMine) _inputActions.Disable();
        }

        private void FixedUpdate()
        {
            if (!_photonView.IsMine) return;
#if UNITY_EDITOR
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                _isTouching = true;
                _startTouchPos = Mouse.current.position.ReadValue();
            }
            else if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                _isTouching = false;
            }

            if (_isTouching)
            {
                Vector2 currentTouch = Mouse.current.position.ReadValue();
#else
            if (_inputActions.Player.JoystickTouchPhase.IsPressed())
            {
                if (!_isTouching)
                {
                    _isTouching = true;
                    _startTouchPos = _inputActions.Player.JoystickTouch.ReadValue<Vector2>();
                }

                Vector2 currentTouch = _inputActions.Player.JoystickTouch.ReadValue<Vector2>();
#endif
                Vector2 delta = currentTouch - _startTouchPos;

                if (delta.magnitude > 20f)
                {
                    Vector3 dir = new Vector3(delta.x, 0, delta.y).normalized;

                    if (_rigidbody.velocity.magnitude < maxSpeed)
                    {
                        _rigidbody.AddForce(dir * movePower, ForceMode.Force);
                    }
                }
            }
#if !UNITY_EDITOR
            else
            {
                _isTouching = false;
                
            }
#endif
            _rigidbody.velocity *= drag;
        }
    }
}
