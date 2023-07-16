using MiProduction.BroAudio.Data;
using MiProduction.Extension;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Runtime
{
	public class SeamlessLoopHelper
	{
		private Func<AudioPlayer> _getPlayerFunc;
		private AudioPlayerInstanceWrapper _playerWrapper;

		public SeamlessLoopHelper(AudioPlayerInstanceWrapper playerWrapper,Func<AudioPlayer> getPlayerFunc)
		{
			_getPlayerFunc = getPlayerFunc;
			_playerWrapper = playerWrapper;
		}

		public void SetPlayer(AudioPlayer player)
		{
			player.OnFinishingStarted += OnFinishingStarted;
		}

		private void OnFinishingStarted(int id, BroAudioClip clip, PlaybackPreference pref)
		{
			var newPlayer = _getPlayerFunc?.Invoke();
			_playerWrapper.UpdateInstance(newPlayer);

			var audioType = Utility.GetAudioType(id);
			if(SoundManager.Instance.AudioTypePref.TryGetValue(audioType,out var audioTypePref))
			{
				newPlayer.SetEffect(audioTypePref.EffectType, SetEffectMode.Override);
				newPlayer.SetVolume(audioTypePref.Volume, 0f);
			}
			newPlayer.Play(id, clip, pref);
			newPlayer.OnFinishingStarted += OnFinishingStarted;
		}
	}
}