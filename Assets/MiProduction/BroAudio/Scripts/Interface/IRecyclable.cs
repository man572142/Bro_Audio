using System;
using System.Collections.Generic;

namespace MiProduction.BroAudio
{
	public interface IRecyclable<T> where T : IRecyclable<T>
	{
		public event Action<T> OnRecycle;
	} 
}