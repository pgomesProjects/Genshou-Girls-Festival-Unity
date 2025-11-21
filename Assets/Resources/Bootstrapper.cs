using UnityEngine;

public static class Bootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        Object.DontDestroyOnLoad(Object.Instantiate(Resources.Load("Init")));
    }
}
