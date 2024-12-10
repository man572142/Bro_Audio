using UnityEngine;
using UnityEngine.Playables;

namespace Ami.BroAudio.Demo
{
	public class CutScenePlayer : InteractiveComponent
	{
		[SerializeField] PlayableDirector _director = null;
		[SerializeField] SoundID _backgroundMusic = default;
		[SerializeField, Volume(true)] float _maxBgmVolumeDuringCutScene = default;

		protected override bool IsTriggerOnce => true;

		public override void OnInZoneChanged(bool isInZone)
		{
			base.OnInZoneChanged(isInZone);

			_director.Play();
			_director.stopped += OnCutSceneStopped;
            BroAudio.Play(_backgroundMusic)
                .AsBGM()
                .SetVolume(_maxBgmVolumeDuringCutScene);
		}

        private void OnCutSceneStopped(PlayableDirector director)
		{
			_director.stopped -= OnCutSceneStopped;
			BroAudio.SetVolume(_backgroundMusic,1f,2f);
		}
	}
}