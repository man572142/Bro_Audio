using System;
using System.Collections.Generic;

namespace MiProduction.BroAudio.Core
{
	public interface IRecyclable<T> where T : IRecyclable<T>
	{
		public event Action<T> OnRecycle;
	} 
}