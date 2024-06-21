namespace Ami.BroAudio
{
	public interface IPitchSettable
	{
#if UNITY_2020_2_OR_NEWER
		internal IAudioPlayer SetPitch(float pitch, float fadeTime);
#else
        IAudioPlayer SetPitch(float pitch, float fadeTime);
#endif
	}
}