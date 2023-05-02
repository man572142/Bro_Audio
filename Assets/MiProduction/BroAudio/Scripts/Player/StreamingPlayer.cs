
namespace MiProduction.BroAudio.Core
{
    public class StreamingPlayer : AudioPlayer
    {
        protected override void Start()
        {
            ClipVolume = 0f;
            base.Start();
        }
	}
}