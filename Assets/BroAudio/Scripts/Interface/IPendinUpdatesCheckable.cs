using System.Collections.Generic;

namespace MiProduction.BroAudio.Editor
{
	public interface IPendinUpdatesCheckable
	{
		void CheckChanges(IChangesTrackable changes);

		void CheckChanges(IEnumerable<IChangesTrackable> changes);
	}

}