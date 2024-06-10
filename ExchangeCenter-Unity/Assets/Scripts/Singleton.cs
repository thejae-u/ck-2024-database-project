using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                Init();
            }

            return instance;
        }
    }

    private static void Init()
    {
        instance = FindObjectOfType<T>();
        if (instance == null)
        {
            Debug.LogError($"{typeof(T)} not found");
            return;
        }

        instance = instance.GetComponent<T>();
    }
}
