namespace Ami.BroAudio.Editor
{
	public interface IChangesTrackable
	{
		void CommitChanges();
		bool IsDirty();
		void DiscardChanges();
		int ChangedID { get; set; }
	}

}