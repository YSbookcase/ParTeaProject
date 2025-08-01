using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using JTW_JumpGame;
using System.Linq;

public class JumpPlayerController : MonoBehaviourPun
{
    [SerializeField] private List<Renderer> colorRenderers;

    [SerializeField] private GameObject nicknamePanel;
    [SerializeField] private float jumpPower = 7f;
    private Rigidbody rigid;

    private bool isGround = true;

    private Color[] playerColors = new Color[]
    {
        Color.red, Color.blue, Color.green, Color.yellow
    };

    private void Awake()
    {
        rigid = GetComponent<Rigidbody>();

        GameObject gameCanvas = GameObject.Find("JumpGameUI");

        GameObject nicknamePanelObj = Instantiate(nicknamePanel, gameCanvas.transform);

        NicknamePanel panel = nicknamePanelObj.GetComponent<NicknamePanel>();
        panel.SetInfo(photonView.Owner.NickName, transform);

        object colorIndex;
        if(photonView.Owner.CustomProperties.TryGetValue("Color", out colorIndex))
        {
            colorRenderers.ForEach(r => r.material.color = playerColors[(int)colorIndex]);
        }
    }

    public void OnJump(InputValue value)
    {
        if (!photonView.IsMine) return;
        if (!isGround) return;

        photonView.RPC("JumpGame_Jump", RpcTarget.All);
        isGround = false;
    }

    [PunRPC]
    private void JumpGame_Jump(PhotonMessageInfo info)
    {
        float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));

        rigid.velocity = Vector3.up * jumpPower;

        // 지연 보상을 위해 위치와 속도 값 계산 및 반영
        rigid.position += 0.5f * Physics.gravity * lag * lag + rigid.velocity * lag;
        rigid.velocity += Physics.gravity * lag;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (!photonView.IsMine) return;

        if (other.gameObject.CompareTag("Finish"))
        {
            isGround = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!photonView.IsMine) return;

        if (other.gameObject.CompareTag("Respawn"))
        {
            photonView.RPC("JumpGamePlayerOut", RpcTarget.All);
        }
    }

    [PunRPC]
    private void JumpGamePlayerOut()
    {
        Destroy(gameObject);
    }
}
