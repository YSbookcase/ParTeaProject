using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    public static void CreateInstance()
    {
        if(instance == null)
        {
            T prefab = Resources.Load<T>(typeof(T).Name);
            instance = Instantiate(prefab);
            DontDestroyOnLoad(instance.gameObject);
        }
    }

    public static void ReleaseInstance()
    {
        if(instance != null)
        {
            Destroy(instance.gameObject);
            instance = null;
        }
    }

    public static T GetInstance()
    {
        return instance;
    }
}
