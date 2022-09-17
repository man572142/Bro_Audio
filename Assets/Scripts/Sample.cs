using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MiProduction.BroAudio;

public class Sample : MonoBehaviour
{
    //[SerializeField] Transition _transitionA = Transition.Immediate;
    //[SerializeField] Transition _transitionB = Transition.Immediate;

    //[SerializeField] float _crossFadeTime = -1f;

    //[SerializeField] Transform _scenePlayer = null;

    public void PlayA()
    {
        //SoundManager.Instance.PlayMusic(Music.MusicA,_transitionA,_crossFadeTime);
    }
    
    public void PlayB()
    {
        //SoundManager.Instance.PlayMusic(Music.MusicB, _transitionB,_crossFadeTime);
    }

    public void PlaySFX()
    {
        //SoundManager.Instance.PlaySFX(Sound.None);
    }

    public void PlaySceneSFX()
    {
        //SoundManager.Instance.PlaySFX(Sound.SceneSound,_scenePlayer.position);
    }

    public void PlayRandomSound()
    {
        //SoundManager.Instance.PlayRandomSFX(Sound.Random);
    }
}
