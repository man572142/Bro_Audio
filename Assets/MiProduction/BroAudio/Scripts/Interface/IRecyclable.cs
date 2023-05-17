﻿using System;
using System.Collections.Generic;

namespace MiProduction.BroAudio.Runtime
{
	public interface IRecyclable<T> where T : IRecyclable<T>
	{
		public event Action<T> OnRecycle;
	} 
}