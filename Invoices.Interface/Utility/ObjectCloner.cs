using System;
using System.Collections.Generic;
using System.Reflection;

namespace Invoices.Interface.Utility;

public static class ObjectCloner
{
	public static T Clone<T>(T obj)
	{
		var clone = Activator.CreateInstance<T>();

		foreach (var property in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public))
		{
			var value = property.GetValue(obj);

			if (value != null)
			{
				var valueType = value.GetType();

				if (valueType.IsGenericType && (valueType.GetGenericTypeDefinition() == typeof(List<>)))
				{
					var cloneMethod = typeof(ObjectCloner).GetMethod("CloneList", BindingFlags.Static | BindingFlags.Public)!.MakeGenericMethod(valueType.GetGenericArguments());

					value = cloneMethod.Invoke(null, new object[] { value });
				}
			}

			if (value is ICloneable cloneableValue)
				value = cloneableValue.Clone();

			property.SetValue(clone, value);
		}

		return clone;
	}

	public static List<T> CloneList<T>(List<T> list) => new List<T>(list);
}
