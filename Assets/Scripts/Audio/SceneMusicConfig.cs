using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="SceneMusicConfig", menuName = "BroAudio/Scene Music Config")]
public class SceneMusicConfig : ScriptableObject
{
    public SceneMusic[] musicScenes;
    [Min(0)]
    public int element = 0;
}
