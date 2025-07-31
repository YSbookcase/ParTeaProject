using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

[System.Serializable]
[CreateAssetMenu(fileName = "AudioData", menuName = "Audio/Data")]
public class AudioData : ScriptableObject
{
    public string clipName;
    public AssetReferenceT<AudioClip> clipRef;
    [Range(0f, 1f)]
    public float volume = 1.0f;
    public bool loop = false;
}
