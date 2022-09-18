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
    // 未完成，目前仍然要手動選enum
    static PrefabUpdater()
    {
        //PrefabStage.prefabStageClosing += OnPrefabClosing;
        //PrefabStage.prefabSaved += OnPrefabSaved;
        PrefabStage.prefabStageDirtied += OnPrefabDirtied;
        //PrefabStage.prefabStageOpened += OnPrefabOpened;
    }

    private static void OnPrefabOpened(PrefabStage stage)
    {
        //using (var editingScope = new PrefabUtility.EditPrefabContentsScope(stage.assetPath))
        //{
        //    Debug.Log("Call");
        //    GameObject prefab = editingScope.prefabContentsRoot;
        //    Debug.Log(stage.openedFromInstanceRoot == null);
        //    if (prefab != null && prefab.TryGetComponent(out SoundManager soundManager))
        //    {
        //        string[] soundNameList =
        //            soundManager.SoundLibraries.Select(x => x.Name)
        //            .Concat(soundManager.RandomSoundLibraries.Select(x => x.Name))
        //            .ToArray();
        //        string[] musicNameList = soundManager.MusicLibraries.Select(x => x.Name).ToArray();

        //        EnumGenerator.Generate("Sound", soundNameList);
        //        EnumGenerator.Generate("Music", musicNameList);

        //        for (int i = 0; i < soundManager.SoundLibraries.Length; i++)
        //        {
        //            if (Enum.TryParse(soundManager.SoundLibraries[i].Name, out Sound result))
        //            {
        //                Debug.Log("ASSIGN ENUM SUCESS " + result.ToString());
        //                //PrefabUtility.RecordPrefabInstancePropertyModifications(prefab);
        //                soundManager.SoundLibraries[i].Sound = result;

        //            }
        //        }
        //    }
        //    PrefabUtility.SavePrefabAsset(prefab);
        //}
    }

    private static void OnPrefabDirtied(PrefabStage stage)
    {
        GameObject prefab = stage.openedFromInstanceRoot;

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
                    //Debug.Log("ASSIGN ENUM SUCESS " + result.ToString());
                    //PrefabUtility.RecordPrefabInstancePropertyModifications(prefab);
                    soundManager.SoundLibraries[i].Sound = result;

                }
            }
            //    PrefabUtility.SavePrefabAsset(prefab);
        }
    }

    private static void OnPrefabSaved(GameObject prefab)
    {

    }

    public static void OnPrefabClosing(PrefabStage stage)
    {

    }


} 
#endif
