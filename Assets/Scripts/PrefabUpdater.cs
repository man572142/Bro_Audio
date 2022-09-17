using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using MiProduction.BroAudio;
using System.Linq;
using System;

#if UNITY_EDITOR
[InitializeOnLoad]
public class PrefabUpdater
{
    static PrefabUpdater()
    {
        PrefabStage.prefabStageClosing += OnPrefabClosing;
    }

    public static void OnPrefabClosing(PrefabStage stage)
    {
        Debug.Log("Prefab Closing");
        GameObject prefab = stage.prefabContentsRoot;

        if (prefab != null && prefab.TryGetComponent(out SoundManager soundManager))
        {
            string[] soundNameList =
                soundManager.SoundLibraries.Select(x => x.Name)
                .Concat(soundManager.RandomSoundLibraries.Select(x => x.Name))
                .ToArray();
            string[] musicNameList = soundManager.MusicLibraries.Select(x => x.Name).ToArray();

            EnumGenerator.Generate("Sound", soundNameList);
            EnumGenerator.Generate("Music", musicNameList);

            for (int i = 0; i < soundManager.SoundLibraries.Length; i++)
            {
                if (Enum.TryParse(soundManager.SoundLibraries[i].Name, out Sound result))
                {
                    Debug.Log("ASSIGN ENUM SUCESS " + result.ToString());
                    soundManager.SoundLibraries[i].Sound = result;
                }
            }
        }
    }
} 
#endif
