using System;
using UnityEngine;
using static MiProduction.Extension.AudioExtension;

namespace MiProduction.Extension
{
	public class AudioClipEditingHelper : IDisposable
	{
		private float[] _sampleDatas = null;
		private AudioClip _originalClip = null;
		public bool HasEdited { get; private set; }

		public AudioClipEditingHelper(AudioClip originalClip)
		{
			_originalClip = originalClip;
		}

		public bool IsInit => _originalClip != null;

		public bool CanEdit => IsInit && Samples != null;

		private float[] Samples
		{
			get
			{
				if (_sampleDatas == null && IsInit)
				{
					_sampleDatas = _originalClip.GetSampleData();
				}
				return _sampleDatas;
			}
		}

		public AudioClip GetResultClip()
		{
			if (!IsInit)
			{
				return null;
			}
			else if (!HasEdited)
			{
				return _originalClip;
			}

			return CreateAudioClip(_originalClip.name, Samples, _originalClip.GetAudioClipSetting());
		}

		public void Trim(float startPos, float endPos)
		{
			if (!IsInit)
			{
				return;
			}

			HasEdited = _originalClip.TryGetSampleData(out _sampleDatas, startPos, endPos);
		}

		public void Boost(float boostVolInDb)
		{
			if (!CanEdit)
			{
				return;
			}

			for (int i = 0; i < Samples.Length; i++)
			{
				int sign = 1;
				float vol = Samples[i];
				if (vol < 0)
				{
					sign = -1;
					vol *= sign;
				}

				float db = vol.ToDecibel();
				db += boostVolInDb;

				Samples[i] = db.ToNormalizeVolume() * sign;
			}
			HasEdited = true;
		}

		public void Reverse()
		{
			if (!CanEdit)
			{
				return;
			}

			Array.Reverse(Samples);
			HasEdited = true;
		}

		public void Fade(float startTime, float fadeTime, bool isFadeIn)
		{
			if (!CanEdit)
			{
				return;
			}

			int startSample = startTime != 0f ? Mathf.RoundToInt(startTime * _originalClip.frequency * _originalClip.channels) : 0;
			int fadeSample = Mathf.RoundToInt(fadeTime * _originalClip.frequency * _originalClip.channels);
			int endSample = startSample + fadeSample;

			float volFactor = isFadeIn ? 0f : 1f;
			float volIncrement = 1f / fadeSample;
			if (!isFadeIn)
			{
				volIncrement *= -1f;
			}

			for (int i = startSample; i < endSample; i++)
			{
				Samples[i] *= volFactor;
				volFactor += volIncrement;
			}
		}


		public void Dispose()
		{
			_sampleDatas = null;
			_originalClip = null;
		}
	} 
}