using Ami.Extension;

namespace Ami.BroAudio
{
    [System.Serializable]
	public struct TriggerData
	{
        [BeautyEnum]
        public UnityMessage OnEvent;
		[BeautyEnum]
		public BroAction DoAction;

		public TriggerParameter Parameter;
		
	}
}