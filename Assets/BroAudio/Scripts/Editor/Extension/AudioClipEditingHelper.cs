using System;
using UnityEngine;
using static Ami.Extension.AudioExtension;

namespace Ami.Extension
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

		public bool HasClip => _originalClip != null;

		public bool CanEdit => HasClip && Samples != null;

		private float[] Samples
		{
			get
			{
				if (_sampleDatas == null && HasClip)
				{
					_sampleDatas = _originalClip.GetSampleData();
				}
				return _sampleDatas;
			}
		}

		public AudioClip GetResultClip()
		{
			if (!HasClip)
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
			if (!HasClip)
			{
				return;
			}

			HasEdited = _originalClip.TryGetSampleData(out _sampleDatas, startPos, endPos);
		}

		public void AddSlient(float time)
		{
			if(!CanEdit)
			{
				return;
			}

			int slientSampleLength = (int)(time * _originalClip.frequency * _originalClip.channels);
			float[] newSampleDatas = new float[_sampleDatas.Length + slientSampleLength];
			Array.Copy(_sampleDatas, 0, newSampleDatas, slientSampleLength, _sampleDatas.Length);
			_sampleDatas = newSampleDatas;
			HasEdited = true;
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

				float db = vol.ToDecibel(false);
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

		public void FadeIn(float fadeTime)
		{
			if (!CanEdit)
			{
				Debug.Log("Cant edit");
				return;
			}

			int fadeSample = Mathf.RoundToInt(fadeTime * _originalClip.frequency * _originalClip.channels);

			// TODO: Accept more ease type
			float volFactor = 0f;
			float volIncrement = 1f / fadeSample;

			for (int i = 0; i < fadeSample; i++)
			{
				Samples[i] *= volFactor;
				volFactor += volIncrement;
			}
			HasEdited = true;
		}

		public void FadeOut(float fadeTime)
		{
			if (!CanEdit)
			{
				return;
			}

			int fadeSample = Mathf.RoundToInt(fadeTime * _originalClip.frequency * _originalClip.channels);
			int startSampleIndex = Samples.Length - fadeSample;

			// TODO: Accept more ease type
			float volFactor = 1f;
			float volIncrement = 1f / fadeSample * -1f;


			for (int i = startSampleIndex; i < Samples.Length; i++)
			{
				Samples[i] *= volFactor;
				volFactor += volIncrement;
			}
			HasEdited = true;
		}

		public void Dispose()
		{
			_sampleDatas = null;
			_originalClip = null;
		}
	} 
}