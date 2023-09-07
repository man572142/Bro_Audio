namespace Ami.BroAudio.Data
{
	public class MusicLibraryAsset : AudioAsset<AudioLibrary>
	{
		public override BroAudioType AudioType => BroAudioType.Music;
	}
}
