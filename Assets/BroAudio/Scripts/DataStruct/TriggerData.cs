using Ami.Extension;

namespace Ami.BroAudio
{
    [System.Serializable]
	public struct TriggerData
	{
        [BeautifulEnum]
        public UnityMessage OnEvent;
		[BeautifulEnum]
		public BroAction DoAction;

		public TriggerParameter Parameter;
		
	}
}