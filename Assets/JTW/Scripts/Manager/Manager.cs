using KYS;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Manager
{
    public static GameManager game => GameManager.GetInstance();
    public static KYS.UIManager UI => KYS.UIManager.GetInstance();
    public static PhotonManager Photon => PhotonManager.GetInstance();
    public static FirebaseManager Firebase => FirebaseManager.GetInstance();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initailize()
    {
        GameManager.CreateInstance();
        KYS.UIManager.CreateInstance();
        PhotonManager.CreateInstance();
        FirebaseManager.CreateInstance();
    }
}
