using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Pool;

public class AudioManager : Singleton<AudioManager>
{
    public ObjectPool<SfxController> sfxPool { get; private set; }

    public float masterVolume = 1f;
    private float bgmVolumePrivate = 1f;
    public float bgmVolume
    {
        get => bgmVolumePrivate;
        set
        {
            bgmVolumePrivate = value;
            bgmSource.volume = masterVolume * bgmVolumePrivate * bgmLocalVolume;
        }
    }
    private float bgmLocalVolume;
    public float sfxVolume = 1f;

    private AudioSource bgmSource;

    //루프사운드 관리하는 딕셔너리
    private Dictionary<string, SfxController> loopingSfxDict = new Dictionary<string, SfxController>();
    private Dictionary<string, AudioData> loopingSfxDataDict = new Dictionary<string, AudioData>();

    private AudioData curBgmData;

    private void Awake()
    {
        bgmSource = gameObject.GetOrAddComponent<AudioSource>();
        bgmSource.loop = true;

        sfxPool = new ObjectPool<SfxController>(CreateSfx, GetSfx, ReleaseSfx, DestroySfx);
    }

    public void BgmPlay(string clipName, float fadeDuration = 0)
    {
        if (clipName == null)
        {
            bgmSource.Stop();
            bgmSource.clip = null;
            if (curBgmData != null)
            {
                Resources.UnloadAsset(curBgmData);
            }
            return;
        }

        AudioData data = Resources.Load<AudioData>($"Audio/{clipName}");

        if(data == null)
        {
            Debug.Log($"[AudioManager] {clipName} AudioData를 찾을 수 없습니다.");
            return;
        }

        AudioClip clip = data.clip;

        if (bgmSource.clip == clip) return;

        bgmLocalVolume = data.volume;
        bgmSource.DOKill();
        bgmSource.DOFade(0f, fadeDuration).OnComplete(() =>
        {
            bgmSource.Stop();
            bgmSource.clip = clip;
            bgmSource.Play();

            bgmSource.DOFade(masterVolume * bgmVolume * data.volume, fadeDuration);

            if(curBgmData != null)
            {
                Resources.UnloadAsset(curBgmData);
            }
            curBgmData = data;
        });
    }



    public void SfxPlay(string clipName, Transform parent = null)
    {
        if(parent == null)
        {
            parent = Camera.main.transform;
        }

        AudioData data = Resources.Load<AudioData>($"Audio/{clipName}");

        if (data == null)
        {
            Debug.Log($"[AudioManager] {clipName} AudioData를 찾을 수 없습니다.");
            return;
        }

        SfxController sfx = sfxPool.Get();
        sfx.transform.parent = parent;
        sfx.transform.localPosition = Vector3.zero;
        sfx.SfxPlay(data, Mathf.Clamp01(masterVolume * sfxVolume * data.volume));
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

    public void SfxPlayLoop(string key, string clipName, Transform parent)
    {
        if (loopingSfxDict.ContainsKey(key))
            return;

        AudioData data = Resources.Load<AudioData>($"Audio/{clipName}");

        if (data == null)
        {
            Debug.Log($"[AudioManager] {clipName} AudioData를 찾을 수 없습니다.");
            return;
        }

        SfxController sfx = sfxPool.Get();
        sfx.transform.parent = parent;
        sfx.transform.localPosition = Vector3.zero;

        AudioSource source = sfx.GetComponent<AudioSource>();
        source.clip = data.clip;
        source.volume = Mathf.Clamp01(masterVolume * sfxVolume * data.volume);
        source.loop = true;
        source.Play();

        loopingSfxDict[key] = sfx;
        loopingSfxDataDict[key] = data;
    }

    public void SetVolumeLoopSfx(string key, float volume)
    {
        if (loopingSfxDataDict.ContainsKey(key))
        {
            Debug.Log($"해당 {key} 의 LoopSound가 없습니다.");
            return;
        }

        loopingSfxDataDict[key].GetComponent<AudioSource>().volume = volume;
    }

    public void SfxStopLoop(string key)
    {
        if (!loopingSfxDict.TryGetValue(key, out SfxController sfx))
            return; // 이미 Release된 상태

        AudioSource source = sfx.GetComponent<AudioSource>();

        source.Stop();
        source.loop = false;
        source.volume = Mathf.Clamp01(masterVolume * sfxVolume);
        sfxPool.Release(sfx);
        loopingSfxDict.Remove(key);
        Resources.UnloadAsset(loopingSfxDataDict[key]);
        loopingSfxDataDict.Remove(key);
    }
}
