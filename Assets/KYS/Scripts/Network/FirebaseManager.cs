using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace KYS
{
    public class FirebaseManager : Singleton<FirebaseManager>
    {
        private static FirebaseManager instance;
        public static FirebaseManager Instacne { get { return instance; } }

        private static FirebaseApp app;
        public static FirebaseApp App { get { return app; } }

        private static FirebaseAuth auth;
        public static FirebaseAuth Auth { get { return auth; } }

        // 초기화 완료 이벤트 추가
        public static event Action OnFirebaseInitialized;
        public static bool IsInitialized { get; private set; } = false;

        //[Header("Firebase 설정")]
        //[SerializeField] private bool useAuthEmulator = false; // 실제 Firebase 서비스 사용

        protected override void Awake() => base.Awake();

        private void Start()
        {
            Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                Firebase.DependencyStatus dependencyStatus = task.Result;
                if (dependencyStatus == Firebase.DependencyStatus.Available)
                {
                    Debug.Log("파이어 베이스 설정이 모두 충족되어 사용할 수 있는 상황");
                    app = FirebaseApp.DefaultInstance;
                    auth = FirebaseAuth.DefaultInstance;

                    IsInitialized = true;
                    OnFirebaseInitialized?.Invoke(); // 초기화 완료 이벤트 발생
                }
                else
                {
                    Debug.LogError($"파이어 베이스 설정이 충족되지 않아 실패했습니다. 이유 {dependencyStatus}");
                    app = null;
                    auth = null;
                    IsInitialized = false;
                }
            });
        }


    }
}