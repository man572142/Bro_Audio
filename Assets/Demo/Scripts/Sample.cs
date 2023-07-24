using System.Collections;
using UnityEngine;
using MiProduction.BroAudio;
using MiProduction.Extension;

using System;

public class Sample : MonoBehaviour
{
	private enum Side { A,B,}

    [SerializeField] AudioID _musicA;
    [SerializeField] AudioID _musicB;
	[SerializeField] AudioID _seamlessAmb;
    [SerializeField] AudioID _uiClick;
    [SerializeField] AudioID _voiceOver;

	private IMusicPlayer _currentMusicPlayer = null;
	private Side _currentSide = Side.A;

	private Transition _transitionMode = Transition.Default;

	void Start()
    {
		BroAudio.Play(_seamlessAmb);
		//StartCoroutine(PlayTest());
    }

	private IEnumerator PlayTest()
    {
		Debug.Log("Play _music1");
        BroAudio.Play(_musicA).AsBGM();
        yield return new WaitForSeconds(5f);

		//Debug.Log("Set effect lowPass");
		//var effect = new EffectParameter(EffectType.LowPass);
		//effect.FadeTime = 2f;
		//effect.FadingEase = Ease.OutCubic;
		//effect.Value = 800f;
		//var xorUI = BroAudioType.All ^ BroAudioType.UI;
		//BroAudio.SetEffect(effect, xorUI).ForSeconds(5f);
		//yield return new WaitForSeconds(2f);

		//Debug.Log("Play _voiceOver as invader");
		//BroAudio.Play(_voiceOver)
		//	.AsInvader().QuietOthers(0.3f,BroAdvice.FadeTime_Smooth);

		//yield return new WaitForSeconds(3f);

		//Debug.Log("Play _music1");
		//BroAudio.Play(_musicA)
		//	.AsBGM().SetTransition(Transition.Default, StopMode.Mute, 2f);

		//Debug.Log("Set effect lowPass");
		//BroAudio.SetEffect(effect, xorUI).ForSeconds(3f);

		//yield return new WaitForSeconds(5f);

		//Debug.Log("Set effect HighPass");
		//effect.Type = EffectType.HighPass;
		//effect.Value = 2000f;
		//BroAudio.SetEffect(effect, xorUI).ForSeconds(5f);

		//yield return new WaitForSeconds(7f);

		//Debug.Log("Set effect Vol");
		//effect.Type = EffectType.Volume;
		//effect.Value = 0.5f;
		//BroAudio.SetEffect(effect, xorUI).ForSeconds(5f);
		//Debug.Log("Set effect none");
		//effect.Type = EffectType.None;
		//BroAudio.SetEffect(effect);

		//BroAudio.Play(_voiceOver).WithEffect().LowPassOthers();
		//yield return new WaitForSeconds(6.826f);

		//Debug.Log("DUCK");
		//BroAudio.Play(_voiceOver).WithEffect().QuietOthers(0.3f);
		//yield return new WaitForSeconds(1f);

		//Debug.Log("DUCK");
		//BroAudio.Play(_voiceOver).WithEffect().QuietOthers(0.2f);
		//yield return new WaitForSeconds(1f);

		//Debug.Log("DUCK");
		//BroAudio.Play(_voiceOver).WithEffect().QuietOthers(0.1f);
		//yield return new WaitForSeconds(1f);

		//Debug.Log("A CrossFade");
		//BroAudio.Play(_music1).AsMusic().SetTransition(Transition.CrossFade);
		//yield return new WaitForSeconds(3.27f);

		//BroAudio.Play(_voiceOver).WithEffect().HighPassOthers();
		//yield return new WaitForSeconds(7.75f);

		//BroAudio.Play(_voiceOver).WithEffect().LowPassOthers();
		//yield return new WaitForSeconds(6.826f);

		//Debug.Log("DUCK");
		//BroAudio.Play(_voiceOver).WithEffect().QuietOthers(0.3f);

		//Debug.Log("B Default");
		//BroAudio.Play(_music2)
		//	.AsMusic().SetTransition(Transition.Default);
		//yield return new WaitForSeconds(3.27f);

		//BroAudio.Play(_voiceOver).WithEffect().HighPassOthers();
		//yield return new WaitForSeconds(6.826f);

		//Debug.Log("DUCK");
		//BroAudio.Play(_voiceOver).WithEffect().QuietOthers(0.3f);
		//yield return new WaitForSeconds(3.27f);

		//BroAudio.Play(_voiceOver).WithEffect().HighPassOthers();

		//yield return new WaitForSeconds(5f);

		//Debug.Log("A OnlyFadeOut");
		//BroAudio.Play(_music1).AsMusic().SetTransition(Transition.OnlyFadeOut);

		//yield return new WaitForSeconds(8f);

		//Debug.Log("B OnlyFadeIn");
		//BroAudio.Play(_music2)
		//	.AsMusic().SetTransition(Transition.OnlyFadeIn, StopMode.Pause, 3f);

		//yield return new WaitForSeconds(8f);

		//Debug.Log("A Immediate");
		//BroAudio.Play(_music1).AsMusic().SetTransition(Transition.Immediate).SetVolume(0.5f);
	}

	private void Update()
	{
		RotateIfIsPlaying();
	}

	private void RotateIfIsPlaying()
	{
		//if(_currentMusicPlayer != null)
		//{
		//	_currentMusicPlayer 
		//}
	}

	public void PlayMusic()
	{
		AudioID audioID = _currentSide switch 
		{ 
			Side.A => _musicA, 
			Side.B => _musicB, 
			_ => throw new NotImplementedException(), 
		};
        _currentMusicPlayer = BroAudio.Play(audioID).AsBGM().SetTransition(_transitionMode, 3f);
    }

    public void PlayUI()
	{
        BroAudio.Play(_uiClick);
    }

	// Trigger by button
	public void PlayVO()
	{
        BroAudio.Play(_voiceOver);
    }

	// Trigger by button
	public void Stop()
	{
		BroAudio.Stop(BroAudioType.All);
	}

	// Trigger by slider
	public void SetTransition(float mode)
	{
		_transitionMode = (Transition)mode;
	}

	// Trigger by button
	public void Pause()
	{
		throw new NotImplementedException();
		// 沒有Pause
	}

	// Trigger by button
	public void FlipCassette()
	{
		_currentSide = _currentSide == Side.A ? Side.B : Side.A; 
	}
}
