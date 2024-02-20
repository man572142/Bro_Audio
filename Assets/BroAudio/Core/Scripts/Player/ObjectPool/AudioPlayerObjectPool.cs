using UnityEngine.Audio;
using UnityEngine;
using Ami.Extension;
using System.Collections.Generic;

namespace Ami.BroAudio.Runtime
{
	public class AudioPlayerObjectPool : ObjectPool<AudioPlayer>
	{
		private Transform _parent = null;
		private List<AudioPlayer> _currentPlayers = null;
		private IAudioMixer _mixer = null;

		public AudioPlayerObjectPool(AudioPlayer baseObject, Transform parent, int maxInternalPoolSize, IAudioMixer mixer) : base(baseObject, maxInternalPoolSize)
		{
			_parent = parent;
			_mixer = mixer;
		}

		public override AudioPlayer Extract()
		{
			AudioPlayer player = base.Extract();
#if !UNITY_WEBGL
			player.SetData(_mixer.Mixer, _mixer.GetTrack);
#endif

            _currentPlayers = _currentPlayers ?? new List<AudioPlayer>();
			_currentPlayers.Add(player);
			return player;
		}

		public override void Recycle(AudioPlayer player)
		{
			_mixer.ReturnTrack(player.TrackType,player.AudioTrack);
			RemoveFromCurrent(player);
			base.Recycle(player);
		}

		protected override AudioPlayer CreateObject()
		{
			AudioPlayer newPlayer = GameObject.Instantiate(BaseObject, _parent);
			newPlayer.OnRecycle += Recycle;
			return newPlayer;
		}

		protected override void DestroyObject(AudioPlayer instance)
		{
			GameObject.Destroy(instance.gameObject);
		}

		private void RemoveFromCurrent(AudioPlayer player)
		{
			for(int i = _currentPlayers.Count - 1; i >=0; i--)
			{
				if(_currentPlayers[i] == player)
				{
					_currentPlayers.RemoveAt(i);
				}
			}
		}

		public IReadOnlyList<AudioPlayer> GetCurrentAudioPlayers()
		{
			return _currentPlayers;
        }
	}
}