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

    [SerializeField] float _stopFadeTime = 0f;
    [SerializeField] float _targetVolume = 1f;

    [SerializeField] Transform _scenePlayer = null;

    public void PlayA()
    {
        SoundSystem.PlayMusic(_musicA, _transitionA, _crossFadeTime);
    }

    public void PlayB()
    {
        SoundSystem.PlayMusic(_musicB, _transitionB, _crossFadeTime);
    }

    public void PlaySFX()
    {
        SoundSystem.PlaySound(_sound);
    }

    public void PlaySceneSFX()
    {
        SoundSystem.PlaySound(_sceneSound, _scenePlayer.position);
    }

    public void PlayRandomSound()
    {
        SoundSystem.PlayRandomSFX(_randomSound);
    }

    public void Stop()
    {
        SoundSystem.Stop(_stopFadeTime, AudioType.All);
    }

    public void SetVolume()
    {
        SoundSystem.SetVolume(_targetVolume, AudioType.Music);
    }
}
