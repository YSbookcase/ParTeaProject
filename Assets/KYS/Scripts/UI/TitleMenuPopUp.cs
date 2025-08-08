using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using KYS;
using Photon.Pun;
using Photon.Realtime;

namespace KYS
{

    public class TitleMenuPopUp : BaseUI
    {
        private new void Awake()
        {
            base.Awake();

            // SFX가 포함된 이벤트 등록
            GetBackEvent("BackButton", "SFX_ButtonClickBack").Click += Back;

            // 볼륨 슬라이더 이벤트 등록
            RegisterVolumeSliderEvents();
        }

        private void Start()
        {
            // Firebase 사용자 정보 초기화
            InitializePanel();
            LoadAudioSettings();
        }

        private void OnEnable()
        {
            // Firebase 사용자 정보 업데이트
            if (gameObject.activeInHierarchy)
            {
                RegisterEvents();
                LoadAudioSettings();
            }
        }

        private void OnDisable()
        {

        }

        private void InitializePanel()
        {
            RegisterEvents();
            // 패널이 활성화될 때 로그인 정보 업데이트
        }

        private void RegisterEvents()
        {

        }

        // 볼륨 슬라이더 이벤트 등록
        private void RegisterVolumeSliderEvents()
        {
            // 마스터 볼륨 슬라이더 (주석 처리)
            /*
            var masterSliderEvent = GetEvent("MasterVolumeSlider");
            if (masterSliderEvent != null)
            {
                masterSliderEvent.Down += OnMasterVolumeStartDrag;
                masterSliderEvent.Up += OnMasterVolumeEndDrag;
                Debug.Log("[MenuPopUp] Master 볼륨 슬라이더 이벤트 등록 성공");
            }
            else
            {
                Debug.LogError("[MenuPopUp] MasterVolumeSlider를 찾을 수 없습니다!");
            }
            */

            // BGM 볼륨 슬라이더
            GetEvent("BGMVolumeSlider").Down += OnBGMVolumeStartDrag;
            GetEvent("BGMVolumeSlider").Up += OnBGMVolumeEndDrag;

            // SFX 볼륨 슬라이더
            GetEvent("SFXVolumeSlider").Down += OnSFXVolumeStartDrag;
            GetEvent("SFXVolumeSlider").Up += OnSFXVolumeEndDrag;
        }

        // 볼륨 설정 로드
        private void LoadAudioSettings()
        {
            // PlayerPrefs에서 설정 로드 (기본값 0.5)
            // float masterVol = PlayerPrefs.GetFloat("MasterVolume", 0.5f); // 주석 처리
            float bgmVol = PlayerPrefs.GetFloat("BGMVolume", 0.5f);
            float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 0.5f);

            // UI 업데이트
            // Slider masterSlider = GetUI<Slider>("MasterVolumeSlider"); // 주석 처리
            Slider bgmSlider = GetUI<Slider>("BGMVolumeSlider");
            Slider sfxSlider = GetUI<Slider>("SFXVolumeSlider");

            // if (masterSlider != null) masterSlider.value = masterVol; // 주석 처리
            if (bgmSlider != null) bgmSlider.value = bgmVol;
            if (sfxSlider != null) sfxSlider.value = sfxVol;

            // 오디오 매니저에 적용
            ApplyAudioSettings(0.5f, bgmVol, sfxVol); // masterVol 대신 기본값 0.5f 사용
        }

        // 볼륨 설정 저장
        private void SaveAudioSettings()
        {
            // Slider masterSlider = GetUI<Slider>("MasterVolumeSlider"); // 주석 처리
            Slider bgmSlider = GetUI<Slider>("BGMVolumeSlider");
            Slider sfxSlider = GetUI<Slider>("SFXVolumeSlider");

            // float masterVol = masterSlider != null ? masterSlider.value : 0.5f; // 주석 처리
            float bgmVol = bgmSlider != null ? bgmSlider.value : 0.5f;
            float sfxVol = sfxSlider != null ? sfxSlider.value : 0.5f;

            // PlayerPrefs.SetFloat("MasterVolume", masterVol); // 주석 처리
            PlayerPrefs.SetFloat("BGMVolume", bgmVol);
            PlayerPrefs.SetFloat("SFXVolume", sfxVol);
            PlayerPrefs.Save();
        }

        // 오디오 매니저에 설정 적용
        private void ApplyAudioSettings(float masterVol, float bgmVol, float sfxVol)
        {
            if (Manager.Audio != null)
            {
                // Manager.Audio.masterVolume = masterVol; // 주석 처리
                Manager.Audio.bgmVolume = bgmVol;
                Manager.Audio.sfxVolume = sfxVol;
            }
        }

        // 마스터 볼륨 슬라이더 이벤트 (주석 처리)
        /*
        private void OnMasterVolumeStartDrag(PointerEventData eventData)
        {
            GetEvent("MasterVolumeSlider").Drag += OnMasterVolumeDrag;
        }

        private void OnMasterVolumeEndDrag(PointerEventData eventData)
        {
            GetEvent("MasterVolumeSlider").Drag -= OnMasterVolumeDrag;
            SaveAudioSettings();
        }

        private void OnMasterVolumeDrag(PointerEventData eventData)
        {
            Slider slider = GetUI<Slider>("MasterVolumeSlider");
            if (slider != null && Manager.Audio != null)
            {
                Manager.Audio.masterVolume = slider.value;
                Debug.Log($"[MenuPopUp] Master 볼륨 변경: {slider.value}");
            }
            else
            {
                Debug.LogWarning($"[MenuPopUp] Master 볼륨 변경 실패 - Slider: {slider != null}, AudioManager: {Manager.Audio != null}");
            }
        }
        */

        // BGM 볼륨 슬라이더 이벤트
        private void OnBGMVolumeStartDrag(PointerEventData eventData)
        {
            GetEvent("BGMVolumeSlider").Drag += OnBGMVolumeDrag;
        }

        private void OnBGMVolumeEndDrag(PointerEventData eventData)
        {
            GetEvent("BGMVolumeSlider").Drag -= OnBGMVolumeDrag;
            SaveAudioSettings();
        }

        private void OnBGMVolumeDrag(PointerEventData eventData)
        {
            Slider slider = GetUI<Slider>("BGMVolumeSlider");
            if (slider != null && Manager.Audio != null)
            {
                Manager.Audio.bgmVolume = slider.value;
            }
        }

        // SFX 볼륨 슬라이더 이벤트
        private void OnSFXVolumeStartDrag(PointerEventData eventData)
        {
            GetEvent("SFXVolumeSlider").Drag += OnSFXVolumeDrag;
        }

        private void OnSFXVolumeEndDrag(PointerEventData eventData)
        {
            GetEvent("SFXVolumeSlider").Drag -= OnSFXVolumeDrag;
            SaveAudioSettings();
            
            // SFX 슬라이더 드래그 종료 시 클릭 사운드 재생
            PlayClickSound("SFX_ButtonClick");
        }

        private void OnSFXVolumeDrag(PointerEventData eventData)
        {
            Slider slider = GetUI<Slider>("SFXVolumeSlider");
            if (slider != null && Manager.Audio != null)
            {
                Manager.Audio.sfxVolume = slider.value;
            }
        }


        private void Back(PointerEventData eventData)
        {
            // 팝업 닫기
            UIManager.Instance.ClosePopUp();
        }
    }
}