using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "AudioData", menuName = "Audio/Data")]
public class AudioData : ScriptableObject
{
    public string clipName;
    public AudioClip clip;
    [Range(0f, 1f)]
    public float volume = 1.0f;
}
