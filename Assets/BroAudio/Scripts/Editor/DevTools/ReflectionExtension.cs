using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Ami.Extension
{
	public static class ReflectionExtension
	{
		public static T GetProperty<T>(string propertyName,Type targetType, object obj)
		{
			PropertyInfo property = targetType?.GetProperty(propertyName);
			try
			{
				return (T)property.GetValue(obj);
			}
			catch (InvalidCastException)
			{
				Debug.LogError($"Cast property failed. Property name:{propertyName}");
			}
			catch (NullReferenceException)
			{
				Debug.LogError($"Can't find property in {targetType.Name} with property name:{propertyName}");
			}
			return default(T);
		}

		public static void SetProperty(string propertyName, Type targetType, object target, object value)
		{
			PropertyInfo property = targetType?.GetProperty(propertyName);
			property.SetValue(target, value);
		}

		public static object ExecuteMethod(string methodName, object[] parameter, Type executorType, object executor)
		{
			MethodInfo method = executorType?.GetMethod(methodName);
			return method.Invoke(executor, parameter);
		}
	}

}