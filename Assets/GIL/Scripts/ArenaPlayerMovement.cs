using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
namespace GIL.Scripts
{
    public class ArenaPlayerMovement : MonoBehaviour, IPunObservable
    {
        [Header("Movement Settings")]
        [SerializeField] private float movePower = 30f;
        [SerializeField] private float maxSpeed = 50f;
        [SerializeField] private float rotTorque = 10f;
        [SerializeField] private float drag = 0.95f;
        [SerializeField] private float pushForce = 30f;
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
            
            PhotonNetwork.SendRate = 60;
            PhotonNetwork.SerializationRate = 60;
        }

        private void OnEnable()
        {
            if (_photonView.IsMine) _inputActions.Enable();
        }

        private void OnDisable()
        {
            if (_photonView.IsMine) _inputActions.Disable();
        }

        private void Update()
        {
            if (_photonView.IsMine) return;
        
            _rigidbody.position = _networkPosition;
            _rigidbody.rotation = _networkRotation;
            _rigidbody.velocity = _networkVelocity;
        }
        
        private void FixedUpdate()
        {
            if (_photonView.IsMine)
            {
                HandleInput();
            }
            else
            {
                _rigidbody.MovePosition(_networkPosition);
                _rigidbody.MoveRotation(_networkRotation);
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
                    {
                        _rigidbody.AddForce(dir * movePower, ForceMode.Force);
                        _rigidbody.AddTorque(dir * rotTorque, ForceMode.Force);
                    }
                }
            }
            else
            {
                _isTouching = false;
            }
            _rigidbody.velocity *= drag;
            _rigidbody.angularVelocity *= drag;
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
        
        private void OnCollisionEnter(Collision collision)
        {
            if (!_photonView.IsMine) return;

            if (collision.gameObject.CompareTag("Player"))
            {
                Debug.Log("플레이어 충돌");
                Vector3 pushDir = (collision.transform.position - transform.position).normalized;

                _rigidbody.AddForce(-pushDir * pushForce, ForceMode.Impulse);

                PhotonView otherPhotonView = collision.gameObject.GetComponent<PhotonView>();
                if (otherPhotonView != null)
                {
                    otherPhotonView.RPC(nameof(ArenaApplyPushForce), RpcTarget.AllBuffered, pushDir * pushForce);
                }
            }
        }
        
        [PunRPC]
        public void ArenaApplyPushForce(Vector3 force)
        {
            _rigidbody.AddForce(force, ForceMode.Impulse);
        }

    }
}
