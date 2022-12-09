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

    [SerializeField] UI _uiSound;

    [SerializeField] float _crossFadeTime = -1f;

    [SerializeField] float _stopFadeTime = 0f;
    [SerializeField] float _targetVolume = 1f;

    [SerializeField] Transform _scenePlayer = null;

    public void PlayA()
    {
        BroAudio.PlayMusic(_musicA, _transitionA, _crossFadeTime);
    }

    public void PlayB()
    {
        BroAudio.PlayMusic(_musicB, _transitionB, _crossFadeTime);
    }

    public void PlaySFX()
    {
        BroAudio.PlaySound(_sound);
    }
    
    public void PlayUISound()
	{
        BroAudio.PlaySound(_uiSound);
	}
    public void PlaySceneSFX()
    {
        BroAudio.PlaySound(_sceneSound, _scenePlayer.position);
    }

    public void PlayRandomSound()
    {
        BroAudio.PlayRandomSFX(_randomSound);
    }

    public void Stop()
    {
        BroAudio.Stop(_stopFadeTime, AudioType.All);
    }

    public void SetVolume()
    {
        BroAudio.SetVolume(_targetVolume, AudioType.Music);
    }
}
