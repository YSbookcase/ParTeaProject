using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
namespace GIL.Scripts
{
    public class ArenaPlayerMovement : MonoBehaviour, IPunObservable
    {
        [Header("Movement Settings")]
        [SerializeField] private float movePower = 50f;
        [SerializeField] private float maxSpeed = 15f;
        [SerializeField] private float drag = 0.9f;
        
        private ArenaPlayerActions _inputActions;
        private Rigidbody _rigidbody;
        
        private PhotonView _photonView;

        private Vector2 _startTouchPos;
        private bool _isTouching = false;
        
        private Vector3 _networkPosition;
        private Quaternion _networkRotation;
        private Vector3 _networkVelocity;
        
        private float _lastReceivedTime;
        
        private void Awake()
        {
            _inputActions = new ArenaPlayerActions();
            _rigidbody = GetComponent<Rigidbody>();
            _photonView = GetComponent<PhotonView>();
            
            _networkPosition = transform.position;
            _networkRotation = transform.rotation;
            
            PhotonNetwork.SendRate = 30;
            PhotonNetwork.SerializationRate = 30;
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
            if (_photonView.IsMine)
            {
                HandleInput();
            }
            else
            {
                float lerpFactor = Mathf.Clamp01((Time.time - _lastReceivedTime) * PhotonNetwork.SerializationRate);
                // 다른 플레이어는 부드럽게 보간
                transform.position = Vector3.Lerp(transform.position, _networkPosition, lerpFactor);
                transform.rotation = Quaternion.Lerp(transform.rotation, _networkRotation, lerpFactor);

                // 속도도 보간해서 더 자연스럽게
                _rigidbody.velocity = Vector3.Lerp(_rigidbody.velocity, _networkVelocity, lerpFactor);
            }
        }
        
        private void HandleInput()
        {
            Vector2 pointerPos = _inputActions.Player.JoystickTouch.ReadValue<Vector2>();
            bool isPressed = _inputActions.Player.JoystickTouchPhase.IsPressed();

            if (isPressed)
            {
                if (!_isTouching)
                {
                    _isTouching = true;
                    _startTouchPos = pointerPos;
                }

                Vector2 delta = pointerPos - _startTouchPos;

                if (delta.magnitude > 20f)
                {
                    Vector3 dir = new Vector3(delta.x, 0, delta.y).normalized;

                    if (_rigidbody.velocity.magnitude < maxSpeed)
                        _rigidbody.AddForce(dir * movePower, ForceMode.Force);
                }
            }
            else
            {
                _isTouching = false;
            }
            _rigidbody.velocity *= drag;
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // 내 데이터 전송
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);
                stream.SendNext(_rigidbody.velocity);
            }
            else
            {
                // 다른 플레이어 데이터 수신
                _networkPosition = (Vector3)stream.ReceiveNext();
                _networkRotation = (Quaternion)stream.ReceiveNext();
                _networkVelocity = (Vector3)stream.ReceiveNext();
                _lastReceivedTime = Time.time;
            }
        }
    }
}
