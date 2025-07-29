using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class RacingMap : MonoBehaviour
{
    [SerializeField] public List<RacingLine> racingLines = new List<RacingLine>();

    [SerializeField] CinemachineDollyCart dollyCart;
    public RacingLine startLine;
    public RacingLine goalLine;

    public void SetTrack(int length)
    {
        if (racingLines.Count < length)
        {
            Debug.LogError("Not enough racing lines to set the track length.");
            return;
        }
        
        // 통과해야 하는 Line의 수 설정
        foreach (RacingLine line in racingLines)
        {
            line.havePassLine = length;
        }

        // 랜덤으로 시작 지점 선정
        int startIndex = Random.Range(0, racingLines.Count);
        Debug.Log($"Start index = {startIndex}");
        startLine = racingLines[startIndex];
        startLine.bottomLine.SetActive(true);

        // 시작 지점, 통과해야 할 Line의 수로 Goal 선정
        int goalIndex = (startIndex + length) % racingLines.Count;
        goalLine = racingLines[goalIndex];
        goalLine.goalQuad.SetActive(true);
    }

    public void SetDollyCart(RacingLine selectLine)
    {
        dollyCart.m_Position = selectLine.pathPosition;
    }

}
