using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using static MiProduction.BroAudio.Utility;
using static MiProduction.Extension.CoroutineExtension;
using MiProduction.Extension;
using System;

namespace MiProduction.BroAudio.Runtime
{
	public partial class SoundManager : MonoBehaviour
	{
		public const string MainGroupName = "Main";
		public const string LowPassExposedName = MainGroupName + "_LowPass";
		public const string HighPassExposedName = MainGroupName + "_HighPass";

		public const string ExclusiveTrackName = "Exclusive";

		private AudioMixerGroup _exclusiveTrack = null;
		private int _exclusiveID = 0;
		private Coroutine _mainTrackTweaking = null;

		private AudioMixerGroup _mainGroupTrack = null;
		public AudioMixerGroup MainTrack
		{
			get
			{
				if (!_mainGroupTrack)
				{
					_mainGroupTrack = GetAudioTrack(MainGroupName);
				}
				return _mainGroupTrack;
			}
		}		

		private AudioMixerGroup GetAudioTrack(string trackName)
		{
			var tracks = _broAudioMixer.FindMatchingGroups(trackName);
			if (tracks != null && tracks.Length > 0)
			{
				return tracks[0];
			}
			else
			{
				LogError($"Can't find AudioMixerGroup with name {trackName}.Please re-import BroAudioMixer");
			}
			return null;
		}

		private void InitAuxTrack()
		{

		}

		public bool TryGetExclusiveTrack(int id,out AudioMixerGroup track,out Action onResetExclusive)
		{
			track = null;
			onResetExclusive = OnResetExclusive;
			if (!_exclusiveTrack)
			{
				_exclusiveTrack = GetAudioTrack(ExclusiveTrackName);
			}

			if(_exclusiveID != 0 && id != _exclusiveID)
			{
				// TODO : Accept more exclusive?
				LogError($"There is a exclusive sound:{id.ToName().ToWhiteBold()} playing");
			}
			else
			{
				track = _exclusiveTrack;
				_exclusiveID = id;
			}

			void OnResetExclusive()
			{
				_exclusiveID = 0;
			}

			return track != null;
		}

		public Coroutine SetMainGroupTrackParameter(ExposedParameter paraType,float targetValue, float fadeTime,Ease ease = Ease.Linear)
		{
			string paraName = string.Empty;

			switch (paraType)
			{
				case ExposedParameter.Volume:
					paraName = MainGroupName;
					break;
				case ExposedParameter.LowPass:
					paraName = LowPassExposedName;
					break;
				case ExposedParameter.HighPass:
					paraName = HighPassExposedName;
					break;
			}

			_mainTrackTweaking.StopIn(this);
			_mainTrackTweaking = StartCoroutine(SetTrackParameter(targetValue, fadeTime, ease, paraName));
			return _mainTrackTweaking;
		}

		private IEnumerator SetTrackParameter(float target, float fadeTime, Ease ease, string parameterName)
		{
			if(!_broAudioMixer.GetFloat(parameterName, out float start))
			{
				LogError($"Can't get exposed parameter:{parameterName}. Please re-import BroAudioMixer");
				yield break;
			}

			var values = AnimationExtension.GetLerpValuesPerFrame(start,target,fadeTime,ease);

			foreach(float value in values)
			{
				_broAudioMixer.SetFloat(parameterName, value);
				yield return null;
			}
			_broAudioMixer.SetFloat(parameterName, target);
		}
	}
}
