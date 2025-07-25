using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Invoices.Interface.Utility;

public static class DependencyObjectExtensions
{
	public static IEnumerable<T> FindDescendants<T>(this DependencyObject root)
	{
		for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
		{
			DependencyObject? child;

			try
			{
				child = VisualTreeHelper.GetChild(root, i);
 			}
			catch
			{
				continue;
			}

			if (child is T found)
				yield return found;
			else
				foreach (var result in FindDescendants<T>(child))
					yield return result;
		}
	}
}
