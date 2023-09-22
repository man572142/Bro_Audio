using System.Collections;
using UnityEngine;
using Ami.BroAudio;
using System;
using static Ami.BroAudio.Utility;

public class Sample : MonoBehaviour
{
    [SerializeField] AudioID _musicA = default;
    [SerializeField] AudioID _musicB = default;
	[SerializeField] AudioID _seamlessAmb = default;
    [SerializeField] AudioID _flipTapeSFX = default;
    [SerializeField] AudioID _voiceOver = default;

	[SerializeField] CassetteTape _tape = null;

	private IMusicPlayer _currentMusicPlayer = null;
	private Transition _transitionMode = Transition.Default;

	void Start()
    {
		BroAudio.Play(_musicA);	
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

    // Trigger by button
    public void FlipSide()
	{
		if(_tape)
		{
			_tape.Flip();
			BroAudio.Play(_flipTapeSFX);
			//PlayMusic();
        }
	}

	// Trigger by button
	public void PlayVO()
	{
        BroAudio.Play(_voiceOver);
    }

	// Also trigger by button
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
