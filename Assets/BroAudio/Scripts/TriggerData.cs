namespace Ami.BroAudio
{
	[System.Serializable]
	public struct TriggerData
	{
		public SoundTriggerEvent OnEvent;
		public BroAction DoAction;
	} 
}