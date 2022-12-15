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

	[SerializeField] SFX _sound;
	[SerializeField] SFX _randomSound;
	[SerializeField] SFX _sceneSound;

	[SerializeField] UI _uiSound;

    [SerializeField] VoiceOver _voiceOver;

    [SerializeField] float _standOutRatio = 1f;
    [SerializeField] float _standOutFadeTime = 0f;

    [SerializeField] float _lowpassFreq = 1f;
    [SerializeField] float _lowpassFadeTime = 0f;

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
        //BroAudio.PlayRandomSFX(_randomSound);
    }

    public void Stop()
    {
        BroAudio.Stop(_stopFadeTime, AudioType.All);
    }

    public void SetVolume()
    {
        BroAudio.SetVolume(_targetVolume, AudioType.Music);
    }
    
    public void PlayStandOutSound()
	{
        BroAudio.PlaySound((int)_voiceOver).StandsOut(_standOutRatio,_standOutFadeTime).LowPassOthers(_lowpassFreq,_lowpassFadeTime);

	}
}
