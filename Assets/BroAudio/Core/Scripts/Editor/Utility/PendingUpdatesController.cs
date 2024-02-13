using System.Collections.Generic;

namespace Ami.BroAudio.Editor
{
	public class PendingUpdatesController : IPendinUpdatesCheckable
	{
		private Dictionary<int, IChangesTrackable> _pendingUpdates = new Dictionary<int, IChangesTrackable>();

		public int CurrentCalledNumber { get; private set; } = 0;
		public bool HasPendingUpdates => _pendingUpdates.Count > 0;

		public void CheckChanges(IChangesTrackable updatable)
		{
			if (updatable.IsDirty())
			{
				AddPending(updatable);
			}
			else
			{
				RemovePending(updatable.ChangedID);
			}
		}

		public void CheckChanges(IEnumerable<IChangesTrackable> changesList)
		{
			foreach(var changed in changesList)
			{
				CheckChanges(changed);
			}
		}

		public void CommitAll()
		{
			foreach (IChangesTrackable changes in _pendingUpdates.Values)
			{
				changes.CommitChanges();
				changes.ChangedID = -1;
			}
			_pendingUpdates.Clear();
		}

		public void DiscardAll()
		{
			foreach (IChangesTrackable changes in _pendingUpdates.Values)
			{
				changes.DiscardChanges();
				changes.ChangedID = -1;
			}
			_pendingUpdates.Clear();
		}

		private void AddPending(IChangesTrackable changed)
		{
			if(!_pendingUpdates.ContainsKey(changed.ChangedID))
			{
				CurrentCalledNumber++;
				_pendingUpdates.Add(CurrentCalledNumber, changed);
				changed.ChangedID = CurrentCalledNumber;
			}
		}

		private void RemovePending(int pendingID)
		{
			if(_pendingUpdates.ContainsKey(pendingID))
			{
				_pendingUpdates[pendingID].ChangedID = -1;
				_pendingUpdates.Remove(pendingID);
			}
		}
	}
}