using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SfxController : MonoBehaviour
{
    private AudioSource sfxSource;

    public void Awake()
    {
        sfxSource = gameObject.GetOrAddComponent<AudioSource>();
    }

    public void SfxPlay(AudioData data, float volume)
    {
        sfxSource.Stop();
        sfxSource.clip = data.clip;

        sfxSource.volume = volume;

        sfxSource.Play();

        StartCoroutine(SfxPlayCoroutine(data.clip.length, data));
    }

    private IEnumerator SfxPlayCoroutine(float time, AudioData data)
    {
        yield return new WaitForSeconds(time);

        sfxSource.Stop();
        Resources.UnloadAsset(data);

        Manager.Audio.sfxPool.Release(this);
    }
}
