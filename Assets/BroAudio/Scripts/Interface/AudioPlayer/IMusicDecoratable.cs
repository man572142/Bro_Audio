namespace Ami.BroAudio
{
	public interface IMusicDecoratable
	{
#if UNITY_2020_2_OR_NEWER
        internal IMusicPlayer AsBGM();
#else
        IMusicPlayer AsBGM();
#endif
	} 
}
