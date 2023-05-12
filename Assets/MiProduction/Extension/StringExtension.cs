using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MiProduction.Extension
{
	public static class StringExtension
	{
		public static string SetColor(this string text, Color color)
		{
			string colorString = ColorUtility.ToHtmlStringRGB(color);
			return $"<color=#{colorString}>{text}</color>";
		}

		public static string ToBold(this string text)
		{
			return $"<b>{text}</b>";
		}

		public static string ToItalics(this string text)
		{
			return $"<i>{text}</i>";
		}

		public static string SetSize(this string text, int size)
		{
			return $"<size={size}>{text}</size>";
		}

		public static char ToLower(this char word)
		{
			if(word >= 65 && word <= 90)
			{
				return (char)(word + 32);
			}
			else if(word >= 97 && word <= 122)
			{
				return word;
			}

			return Char.ToLower(word);
		}

		public static char ToUpper(this char word)
		{
			if (word >= 65 && word <= 90)
			{
				return word;
			}
			else if (word >= 97 && word <= 122)
			{
				return (char)(word - 32);
			}

			return Char.ToUpper(word);
		}
	}
}