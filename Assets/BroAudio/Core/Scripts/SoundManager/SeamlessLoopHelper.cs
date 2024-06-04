using System;

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
			_playerWrapper.OnRecycle += Recycle;
		}

        private void Recycle(AudioPlayer player)
        {
            _playerWrapper.OnRecycle -= Recycle;
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
            newPlayer.Play(id, pref);
			newPlayer.OnFinishingOneRound += OnFinishingOneRound;
		}
	}
}