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
            player.OnSeamlessLoopReplay -= Replay;
        }

        public void AddReplayListener(AudioPlayer player)
		{
			player.OnSeamlessLoopReplay += Replay;
		}

		private void Replay(int id, PlaybackPreference pref, EffectType previousEffect, float trackVolume, float pitch)
		{
			var newPlayer = _getPlayerFunc?.Invoke();
			if(newPlayer == null)
			{
				return;
			}

			_playerWrapper.UpdateInstance(newPlayer);

            newPlayer.SetEffect(previousEffect, SetEffectMode.Override);
			newPlayer.SetVolume(trackVolume);
            newPlayer.SetPitch(pitch);
			newPlayer.SetPlaybackData(id, pref);
            newPlayer.Play();
            
            newPlayer.OnSeamlessLoopReplay += Replay;
		}
	}
}