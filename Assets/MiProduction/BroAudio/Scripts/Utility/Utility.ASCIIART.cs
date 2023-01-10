using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MiProduction.BroAudio
{
	public static partial class Utility
	{
		public static string ASCIIFomatter(string text, int lineWidth)
		{
			StringBuilder result = new StringBuilder();
			for (int i = 0; i < text.Length; i++)
			{
				result.Append(text[i]);

				if (i % lineWidth == 0 && i != 0)
				{
					result.Append("\n");
				}
			}
			return result.ToString();
		}
	} 
}
