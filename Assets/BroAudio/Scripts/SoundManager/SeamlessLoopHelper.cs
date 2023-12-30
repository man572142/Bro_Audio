using Ami.BroAudio.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Runtime
{
	public class SeamlessLoopHelper
	{
		private Func<AudioPlayer> _getPlayerFunc;
		private AudioPlayerInstanceWrapper _playerWrapper;

		public SeamlessLoopHelper(AudioPlayerInstanceWrapper playerWrapper,Func<AudioPlayer> getPlayerFunc)
		{
			_getPlayerFunc = getPlayerFunc;
			_playerWrapper = playerWrapper;
			_playerWrapper.OnWrapperRecycle += OnRecycle;
		}

        private void OnRecycle(AudioPlayer player)
        {
            _playerWrapper.OnWrapperRecycle -= OnRecycle;
            player.OnFinishingOneRound -= OnFinishingOneRound;
        }

        public void SetPlayer(AudioPlayer player)
		{
			player.OnFinishingOneRound += OnFinishingOneRound;
		}

		private void OnFinishingOneRound(int id, PlaybackPreference pref, EffectType previousEffect)
		{
			var newPlayer = _getPlayerFunc?.Invoke();
			if(newPlayer == null)
			{
				return;
			}

			_playerWrapper.UpdateInstance(newPlayer);

			var audioType = Utility.GetAudioType(id);
			if(SoundManager.Instance.AudioTypePref.TryGetValue(audioType,out var audioTypePref))
			{
				pref.AudioTypePlaybackPref = audioTypePref;
			}
			newPlayer.SetEffect(previousEffect, SetEffectMode.Override);
            newPlayer.Play(id, pref,false);
			newPlayer.OnFinishingOneRound += OnFinishingOneRound;
		}
	}
}