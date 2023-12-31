using System;
using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T instance
    { 
        get
        {
            if (_instance == null)
            {
                GetInstance();
            }

            return _instance;
        }
    }
    protected static T _instance = null;
    protected static bool instantiated = false;

    protected bool destroyed = false;

    [Header("Base Singleton")]
    public bool dontDestroyOnLoad = true;

    protected virtual void Awake()
    {
        if (destroyed)
            return;

        if (_instance == null)
        {
            SetInstance(this as T);
        }
        else if (_instance != this)
        {
            Debug.LogError("Instance already created.");
            destroyed = true;
            Destroy(gameObject);

            return;
        }
    }

    private void OnDestroy()
    {
        if (destroyed)
            return;

        _instance = null;
    }

    private static void SetInstance(T inst)
    {
        _instance = inst;
        instantiated = true;
        if (inst is Singleton<T> ins)
        {
            ins.destroyed = false;
            if (ins.dontDestroyOnLoad)
                DontDestroyOnLoad(ins);
        }
    }

    private static void GetInstance()
    {
        var objs = FindObjectsOfType<T>();

        if (objs.Length == 0)
        {
            Debug.LogError($"Place the {typeof(T).Name} in the scene.");

            return;
        }
        else if (objs.Length > 1)
        {
            Debug.LogError($"The scene contains more than one {typeof(T).Name}. Unintended behavior can be detected.");
            for (int i = 1; i < objs.Length; i++)
            {
                (objs[i] as Singleton<T>).destroyed = true;
                Destroy(objs[i]);
            }
        }

        SetInstance(objs[0]);
    }
}