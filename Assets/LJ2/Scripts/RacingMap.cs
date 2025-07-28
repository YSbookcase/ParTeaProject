using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class RacingMap : MonoBehaviour
{
    [SerializeField] List<RacingLine> racingLines = new List<RacingLine>();

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
        
        int startIndex = Random.Range(0, racingLines.Count);
        Debug.Log($"Start index = {startIndex}");
        startLine = racingLines[startIndex];
        startLine.gameObject.SetActive(true);

        int goalIndex = (startIndex + length) % racingLines.Count;
        goalLine = racingLines[goalIndex];
    }

    public void SetDollyCart(RacingLine selectLine)
    {
        dollyCart.m_Position = selectLine.pathPosition;
    }

}
