using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.Scene
{
	[CreateAssetMenu(fileName = "SceneConfig_Music", menuName = "MiProduction/SceneConfig/Music")]
	public class SceneConfig_Music : ScriptableObject
	{
		[SerializeField] SceneConfig<Music>[] SceneMusic;

	}

}