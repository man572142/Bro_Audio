using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Ami.Extension
{
	public static class StringExtension
	{
        public static string SetColor(this string text, Color color)
		{
			string colorString = ColorUtility.ToHtmlStringRGB(color);
			return $"<color=#{colorString}>{text}</color>";
		}

		public static string SetColor(this string text, string colorCode)
		{
			return $"<color=#{colorCode}>{text}</color>";
		}

		public static string ToWhiteBold(this string text)
		{
			return text.ToBold().SetColor(Color.white);
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

        public static bool IsEnglishLetter(char word)
        {
            return (word >= 65 && word <= 90) || (word >= 97 && word <= 122);
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

        public static string TrimStartAndEnd(this string text)
        {
            if (Char.IsWhiteSpace(text[0]))
            {
                text = text.TrimStart();
            }
            
			if (Char.IsWhiteSpace(text[text.Length - 1]))
            {
                text = text.TrimEnd();
            }

            return text;
        }
    }
}