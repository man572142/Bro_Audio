namespace MiProduction.BroAudio.Asset.Core
{
	public interface IChangesTrackable
	{
		void CommitChanges();
		bool IsDirty();
		void DiscardChanges();
		int ChangedID { get; set; }
	}

}