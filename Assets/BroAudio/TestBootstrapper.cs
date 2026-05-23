using Ami.BroAudio;
using UnityEngine;

public class TestBootstrapper 
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init()
    {
        var go = new GameObject("TestBootstrapper");
        go.AddComponent<SoundSource>();

    }
}
