using System;
using UnityEngine;

namespace Ami.Extension
{
	[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
	public class EnumSeparator : PropertyAttribute
	{
	}
}