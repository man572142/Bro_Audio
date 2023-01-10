using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

	public static string SetSize(this string text,int size)
	{
		return $"<size={size}>{text}</size>";
	}
}
