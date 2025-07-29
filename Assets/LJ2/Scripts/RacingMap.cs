using Cinemachine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class RacingMap : MonoBehaviour
{
    [SerializeField] public List<RacingLine> racingLines = new List<RacingLine>();

    [SerializeField] CinemachineDollyCart dollyCart;
    [SerializeField] public PhotonView mapView;
    public RacingLine startLine;
    public RacingLine goalLine;
    public int startIndex;

    private void Awake()
    {
        if (mapView == null)
        {
            mapView = GetComponent<PhotonView>();
        }
    }

    //public int SetStartIndex(int length) 
    //{
    //    if (racingLines.Count < length)
    //    {
    //        Debug.LogError("Not enough racing lines to set the track length.");
    //        return -1;
    //    }
    //    // 랜덤으로 시작 지점 선정
    //    startIndex = Random.Range(0, racingLines.Count);
        
    //    return startIndex;
    //}
    [PunRPC]
    public void SetTrack(int length, int startIndex)
    {
        // 통과해야 하는 Line의 수 설정
        foreach (RacingLine line in racingLines)
        {
            line.havePassLine = length;

        }
        Debug.Log($"Track length set to {length} with start index {startIndex}");


        startLine = racingLines[startIndex];
        startLine.bottomLine.SetActive(true);

        // 시작 지점, 통과해야 할 Line의 수로 Goal 선정
        int goalIndex = (startIndex + length) % racingLines.Count;
        goalLine = racingLines[goalIndex];
        goalLine.goalQuad.SetActive(true);
    }

    [PunRPC]
    public void SetDollyCart(int startIndex)
    {
        dollyCart.m_Position = racingLines[startIndex].pathPosition;
        Debug.Log($"DollyCart position set to {dollyCart.m_Position} at start index {startIndex}");
    }

}
