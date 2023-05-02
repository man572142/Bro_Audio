namespace MiProduction.BroAudio.AssetEditor
{
	public interface IChangesTrackable
	{
		void CommitChanges();
		bool IsDirty();
		void DiscardChanges();
		int ChangedID { get; set; }
	}

}