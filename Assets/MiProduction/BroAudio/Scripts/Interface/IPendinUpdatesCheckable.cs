using System.Collections.Generic;

namespace MiProduction.BroAudio.AssetEditor
{
	public interface IPendinUpdatesCheckable
	{
		void CheckChanges(IChangesTrackable changes);

		void CheckChanges(IEnumerable<IChangesTrackable> changes);
	}

}