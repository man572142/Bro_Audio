using System.Collections;
using UnityEngine;
using Ami.BroAudio;
using System;

public class Sample : MonoBehaviour
{
    [SerializeField] AudioID _musicA;
    [SerializeField] AudioID _musicB;
	[SerializeField] AudioID _seamlessAmb;
    [SerializeField] AudioID _flipTapeSFX;
    [SerializeField] AudioID _voiceOver;

	[SerializeField] CassetteTape _tape = null;

	private IMusicPlayer _currentMusicPlayer = null;
	private Transition _transitionMode = Transition.Default;

	void Start()
    {
		BroAudio.Play(_seamlessAmb);
    }

	// Trigger by button
	public void PlayMusic()
	{
		AudioID currentAudioID = GetCurrentSideAudioID();
		_currentMusicPlayer = BroAudio.Play(currentAudioID).AsBGM().SetTransition(_transitionMode, 3f);
    }

	private AudioID GetCurrentSideAudioID()
	{
		switch (_tape.CurrentSide)
		{
			case CassetteTape.Side.A:
				return _musicA;
			case CassetteTape.Side.B:
				return _musicB;
			default:
				return default;
		}
	}

	public void FlipSide()
	{
		if(_tape)
		{
			StopMusic();
			_tape.Flip();
			BroAudio.Play(_flipTapeSFX);
		}
	}

	// Trigger by button
	public void PlayVO()
	{
        BroAudio.Play(_voiceOver);
    }

	// Trigger by button
	public void StopMusic()
	{
		BroAudio.Stop(GetCurrentSideAudioID());
	}

	// Trigger by slider
	public void SetTransition(float mode)
	{
		_transitionMode = (Transition)mode;
	}

	// Trigger by button
	public void Pause()
	{
		BroAudio.Pause(GetCurrentSideAudioID(),0f);
	}
}
