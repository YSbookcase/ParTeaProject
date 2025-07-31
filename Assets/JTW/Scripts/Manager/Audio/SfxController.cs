using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SfxController : MonoBehaviour
{
    private AudioSource _sfxSource;

    public void Awake()
    {
        _sfxSource = gameObject.GetOrAddComponent<AudioSource>();
    }

    public void SfxPlay(AudioClip clip, float volume)
    {
        _sfxSource.Stop();
        _sfxSource.clip = clip;

        _sfxSource.volume = volume;

        _sfxSource.Play();

        StartCoroutine(SfxPlayCoroutine(clip.length));
    }

    private IEnumerator SfxPlayCoroutine(float time)
    {
        yield return new WaitForSeconds(time);

        Manager.Audio.SfxPool.Release(this);
    }
}
