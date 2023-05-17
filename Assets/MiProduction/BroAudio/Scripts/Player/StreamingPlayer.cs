
namespace MiProduction.BroAudio.Runtime
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