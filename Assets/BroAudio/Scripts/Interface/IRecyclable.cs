using System;
using System.Collections.Generic;

namespace Ami.BroAudio.Runtime
{
	public interface IRecyclable<T> where T : IRecyclable<T>
	{
		event Action<T> OnRecycle;
	} 
}