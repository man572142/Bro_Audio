using MiProduction.Scene;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SceneConfig_Sprites", menuName = "MiProduction/SceneConfig/MultipleSprites")]
public class SceneConfig_MultipleSprites : ScriptableObject
{
    public SceneConfig<Sprite[]> SceneSprites;
}
