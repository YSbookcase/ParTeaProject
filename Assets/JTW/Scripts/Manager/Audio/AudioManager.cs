using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Pool;

public class AudioManager : Singleton<AudioManager>
{
    public ObjectPool<SfxController> SfxPool;

    public float MasterVolume = 1f;
    private float _bgmVolume = 1f;
    public float BgmVolume
    {
        get => _bgmVolume;
        set
        {
            _bgmVolume = value;
            _bgmSource.volume = MasterVolume * _bgmVolume * _bgmLocalVolume;
        }
    }
    private float _bgmLocalVolume;
    public float SfxVolume = 1f;

    private AudioSource _bgmSource;

    //루프사운드 관리하는 딕셔너리
    private Dictionary<string, SfxController> _loopingSfxDict = new Dictionary<string, SfxController>();

    private void Awake()
    {
        _bgmSource = gameObject.GetOrAddComponent<AudioSource>();
        _bgmSource.loop = true;

        SfxPool = new ObjectPool<SfxController>(CreateSfx, GetSfx, ReleaseSfx, DestroySfx);
    }

    public void BgmPlay(AudioClip clip, float volume = 1f, float fadeDuration = 0)
    {
        if(clip == null)
        {
            _bgmSource.Stop();
        }

        if (_bgmSource.clip == clip) return;

        _bgmLocalVolume = volume;
        _bgmSource.DOKill();
        _bgmSource.DOFade(0f, fadeDuration).OnComplete(() =>
        {
            _bgmSource.Stop();
            _bgmSource.clip = clip;
            _bgmSource.Play();

            _bgmSource.DOFade(MasterVolume * BgmVolume * volume, fadeDuration);
        });
    }

    public void SfxPlay(AudioClip clip, Transform parent, float volume = 1)
    {
        SfxController sfx = SfxPool.Get();
        sfx.transform.parent = parent;
        sfx.transform.localPosition = Vector3.zero;
        sfx.SfxPlay(clip, Mathf.Clamp01(MasterVolume * SfxVolume * volume));
    }

    private SfxController CreateSfx()
    {
        GameObject obj = new GameObject("SfxController");
        obj.transform.parent = transform;
        AudioSource audioSource = obj.AddComponent<AudioSource>();

        audioSource.spatialBlend = 1.0f;       
        audioSource.minDistance = 5f;        
        audioSource.maxDistance = 30f;      
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;

        return obj.AddComponent<SfxController>();
    }

    private void GetSfx(SfxController sfx)
    {
        sfx.gameObject.SetActive(true);
    }

    private void ReleaseSfx(SfxController sfx)
    {
        sfx.transform.parent = transform;
        sfx.gameObject.SetActive(false);
    }

    private void DestroySfx(SfxController sfx)
    {
        Destroy(sfx.gameObject);
    }


    //루프사운드 플레이
    public void SfxPlayLoop(string key, AudioClip clip, Transform parent, float volume = 1)
    {
        if (_loopingSfxDict.ContainsKey(key))
            return;

        SfxController sfx = SfxPool.Get();
        sfx.transform.parent = parent;
        sfx.transform.localPosition = Vector3.zero;

        AudioSource source = sfx.GetComponent<AudioSource>();
        source.clip = clip;
        source.volume = Mathf.Clamp01(MasterVolume * SfxVolume * volume);
        source.loop = true;
        source.Play();

        _loopingSfxDict[key] = sfx;
    }

    //루프사운드 페이드아웃하면서 멈춤
    public void SfxStopLoop(string key, float fadeDuration = 0.5f)
    {
        if (!_loopingSfxDict.TryGetValue(key, out SfxController sfx))
            return; // 이미 Release된 상태

        AudioSource source = sfx.GetComponent<AudioSource>();
        source.DOKill();

        source.DOFade(0f, fadeDuration).SetEase(Ease.Linear);

        StartCoroutine(StopAndReleaseAfterFade(key, sfx, source, fadeDuration));
    }

    private IEnumerator StopAndReleaseAfterFade(string key, SfxController sfx, AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);

        // 중복 Release 방지
        if (!_loopingSfxDict.ContainsKey(key))
            yield break;

        source.Stop();
        source.loop = false;
        source.volume = Mathf.Clamp01(MasterVolume * SfxVolume);
        SfxPool.Release(sfx);
        _loopingSfxDict.Remove(key);
    }
}
