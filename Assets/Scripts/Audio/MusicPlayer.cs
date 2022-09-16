using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class MusicPlayer : MonoBehaviour
{
	const float MinVolume = -80f;
	const float MaxVolume = 0f;

	[SerializeField] AudioSource _player = null;
	[SerializeField] AudioMixer _audioMixer;
	string _volParaName = string.Empty;
	MusicLibrary _currentMusic;

	Coroutine currentPlayCoroutine;

	public bool IsPlaying { get; private set; }
    public bool IsFadingOut { get; private set; }

    private void Awake()
	{
		if(_player == null)
		{
            _player = GetComponent<AudioSource>();
		}
		if(_audioMixer == null)
		{
			_audioMixer = _player.outputAudioMixerGroup.audioMixer;
		}
		_volParaName = _player.outputAudioMixerGroup.name;
	}

	private void Start()
	{
		if (!_audioMixer.GetFloat(_volParaName, out float currentVol))
		{
			Debug.LogError("[SoundSystem] Can't get exposed parameter in audio mixer,AudioMixerGroup's name and ExposedParameter's name should be the same");
		}
	}


	public void Play(MusicLibrary musicLibrary, float fadeInTime = -1f, float fadeOutTime = -1f, Action onFinishFadeIn = null, Action onFinishPlaying = null)
    {
        currentPlayCoroutine = StartCoroutine(PlayControl(musicLibrary, fadeInTime, fadeOutTime, onFinishFadeIn, onFinishPlaying));
    }

    private IEnumerator PlayControl(MusicLibrary musicLibrary, float fadeInTime, float fadeOutTime, Action onFinishFadeIn, Action onFinishPlaying)
    {
		_audioMixer.SetFloat(_volParaName,MinVolume);
		// WaitForEndOfFrame to prevent pop sound
		yield return new WaitForEndOfFrame();

		_currentMusic = musicLibrary;
        _player.clip = musicLibrary.audioClip;
        //_player.volume = musicLibrary.volume;
        _player.time = musicLibrary.startPosition;
		_player.loop = musicLibrary.loop;
        _player.Play();
        IsPlaying = true;

		do
		{
			#region FadeIn
			fadeInTime = fadeInTime < 0 ? musicLibrary.fadeIn : fadeInTime;
			yield return StartCoroutine(Fade(fadeInTime,musicLibrary.volume));
			onFinishFadeIn?.Invoke();
			#endregion

			#region FadeOut
			fadeOutTime = fadeOutTime < 0 ? musicLibrary.fadeOut : fadeOutTime;
			if (fadeOutTime > 0)
			{
				yield return new WaitUntil(() => (_player.clip.length - _player.time) <= fadeOutTime);
				IsFadingOut = true;
				yield return StartCoroutine(Fade(fadeOutTime,0f));
				IsFadingOut = false;
			}
			else
			{
				yield return new WaitUntil(() => _player.clip.length == _player.time);
			}
			#endregion
		} while (musicLibrary.loop);

		EndPlaying();
		onFinishPlaying?.Invoke();
        
    }

	public void Stop(float fadeOutTime = -1, Action onFinishPlaying = null)
	{
		StartCoroutine(StopControl(fadeOutTime, onFinishPlaying));
	}

	private IEnumerator StopControl(float fadeOutTime, Action onFinishPlaying)
    {
		if (_currentMusic.music == Music.None || !IsPlaying)
		{
			onFinishPlaying?.Invoke();
			yield break;
		}			
		
        fadeOutTime = fadeOutTime < 0 ? _currentMusic.fadeOut : fadeOutTime;
		if (fadeOutTime > 0)
		{
            if(IsFadingOut)
			{
				_player.loop = false;
				yield return new WaitUntil(() => _player.clip.length == _player.time);
			}
            else
			{
				StopCoroutine(currentPlayCoroutine);
				yield return StartCoroutine(Fade(fadeOutTime, 0f));
			}
		}
        else
		{
            StopAllCoroutines();
			_audioMixer.SetFloat(_volParaName, MinVolume);
		}
		EndPlaying();
		onFinishPlaying?.Invoke();
		// 目前設計成:如果原有的音樂已經在FadeOut了，就等它FadeOut不強制停止，除非fadeTime = 0 
	}

	public IEnumerator Fade(float duration, float targetVolume)
	{
		float currentTime = 0;
		float currentVol;
		_audioMixer.GetFloat(_volParaName, out currentVol);
		currentVol = Mathf.Pow(10, currentVol / 20);
		float targetValue = Mathf.Clamp(targetVolume, 0.0001f, 1);
		Ease ease = currentVol < targetValue ? SoundManager.FadeInEase : SoundManager.FadeOutEase;
		while (currentTime < duration)
		{
			currentTime += Time.deltaTime;
			float newVol = Mathf.Lerp(currentVol, targetValue, (currentTime / duration).SetEase(ease));
			_audioMixer.SetFloat(_volParaName, Mathf.Log10(newVol) * 20);
			yield return null;
		}
		yield break;
	}

	private void EndPlaying()
	{
		IsPlaying = false;
		_player.Stop();
		_player.clip = null;
		_player.volume = 1f;
	}

}
