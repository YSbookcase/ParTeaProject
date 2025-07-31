using KYS;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Manager
{
    public static GameManager game => GameManager.GetInstance();
    public static KYS.UIManager UI => KYS.UIManager.GetInstance();
    public static PhotonManager Photon => PhotonManager.GetInstance();
    public static FirebaseManager Firebase => FirebaseManager.GetInstance();
    public static AudioManager Audio => AudioManager.GetInstance();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initailize()
    {
        GameManager.CreateInstance();
        KYS.UIManager.CreateInstance();
        if (SceneManager.GetActiveScene().name == "NetworkScene")
        {
            PhotonManager.CreateInstance();
        }
        FirebaseManager.CreateInstance();
        AudioManager.CreateInstance();
    }
}
