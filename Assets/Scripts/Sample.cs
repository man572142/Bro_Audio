using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MiProduction.BroAudio;

public class Sample : MonoBehaviour
{
    [SerializeField] Transition _transitionA = Transition.Immediate;
    [SerializeField] Transition _transitionB = Transition.Immediate;

    [SerializeField] Music _musicA;
    [SerializeField] Music _musicB;

    [SerializeField] Sound _sound;
    [SerializeField] Sound _randomSound;
    [SerializeField] Sound _sceneSound;

    [SerializeField] float _crossFadeTime = -1f;

    [SerializeField] Transform _scenePlayer = null;

    public void PlayA()
    {
        SoundManager.Instance.PlayMusic(_musicA, _transitionA,_crossFadeTime);
    }
    
    public void PlayB()
    {
        SoundManager.Instance.PlayMusic(_musicB, _transitionB,_crossFadeTime);
    }

    public void PlaySFX()
    {
        SoundManager.Instance.PlaySFX(_sound);
    }

    public void PlaySceneSFX()
    {
        SoundManager.Instance.PlaySFX(_sceneSound,_scenePlayer.position);
    }

    public void PlayRandomSound()
    {
        SoundManager.Instance.PlayRandomSFX(_sceneSound);
    }
}
