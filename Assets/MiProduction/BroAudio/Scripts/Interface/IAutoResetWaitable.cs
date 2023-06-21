using System;
using System.Collections;

namespace MiProduction.BroAudio.Runtime
{
	public interface IAutoResetWaitable
	{
		void Until(Func<bool> predicate);
		void Until(IEnumerator enumerator);
		void While(Func<bool> predicate);
		void ForSeconds(float seconds);
	}

}