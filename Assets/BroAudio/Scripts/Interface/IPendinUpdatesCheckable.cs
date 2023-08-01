using System.Collections.Generic;

namespace Ami.BroAudio.Editor
{
	public interface IPendinUpdatesCheckable
	{
		void CheckChanges(IChangesTrackable changes);

		void CheckChanges(IEnumerable<IChangesTrackable> changes);
	}

}