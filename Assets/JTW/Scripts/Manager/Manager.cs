using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Manager
{
    public static GameManager game => GameManager.GetInstance();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initailize()
    {
        GameManager.CreateInstance();
    }
}
