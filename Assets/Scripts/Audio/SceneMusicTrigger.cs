//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.SceneManagement;


//namespace MiProduction.BroAudio.SceneMusic
//{
//    public class SceneMusicTrigger : MonoBehaviour
//    {
//        [SerializeField] SceneMusicConfig _config = null;
//        [SerializeField] Transition _transition = Transition.FadeOutThenFadeIn;

//        private Music _currentMusic = Music.None;

//        private void Awake()
//        {
//            SceneManager.activeSceneChanged += OnSceneChanged;
//        }

//        private void OnSceneChanged(Scene oldScene, Scene newScene)
//        {
//            foreach(SceneMusic sceneMusic in _config.musicScenes)
//            {
//                if (sceneMusic.Music != _currentMusic && sceneMusic.Scenes.Contains(newScene.name))
//                {
//                    SoundSystem.PlayMusic(sceneMusic.Music,_transition);
//                    _currentMusic = sceneMusic.Music;                    
//                }
//            }

//        }

//        private void OnDestroy()
//        {
//            SceneManager.activeSceneChanged -= OnSceneChanged;
//        }
//    } 
//}
