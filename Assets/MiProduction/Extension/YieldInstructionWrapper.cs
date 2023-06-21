using System.Collections;
using UnityEngine;

namespace MiProduction.Extension
{
	public class YieldInstructionWrapper
	{
		private IEnumerator _enumerator;
		private YieldInstruction _yieldInstruction;

		public bool HasYieldInstruction() => _enumerator != null || _yieldInstruction != null;
		public void SetInstruction(IEnumerator enumerator)
		{
			_enumerator = enumerator;
			_yieldInstruction = null;
		}

		public void SetInstruction(YieldInstruction yieldInstruction)
		{
			_enumerator = null;
			_yieldInstruction = yieldInstruction;
		}

		public IEnumerator Execute()
		{
			if(!HasYieldInstruction())
			{
				Debug.LogWarning("There is no instruction could yield. The execution will do nothing");
				yield break;
			}

			if (_enumerator != null)
			{
				yield return _enumerator;
			}
			else if (_yieldInstruction != null)
			{
				yield return _yieldInstruction;
			}
			_enumerator = null;
			_yieldInstruction = null;
		}
	} 
}