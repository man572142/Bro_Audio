using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct SceneConfig
{
    public string Scene;

    public SceneConfigType ConfigType;

    public Image[] Image;
    public Music Music;


}

public enum SceneConfigType
{
    None = 0,
    Music = 1,
    Image = 2,

}