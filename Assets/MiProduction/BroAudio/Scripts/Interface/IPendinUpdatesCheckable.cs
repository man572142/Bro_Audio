using System.Collections.Generic;

namespace MiProduction.BroAudio.Asset.Core
{
	public interface IPendinUpdatesCheckable
	{
		void CheckChanges(IChangesTrackable changes);

		void CheckChanges(IEnumerable<IChangesTrackable> changes);
	}

}