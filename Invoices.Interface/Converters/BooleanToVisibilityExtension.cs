using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Invoices.Interface.Converters;

using System.Windows;

public class BooleanToVisibilityExtension : MarkupExtension, IValueConverter
{
	public override object ProvideValue(IServiceProvider serviceProvider)
	{
		return this;
	}
	
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is bool isVisible)
			return isVisible ? Visibility.Visible : Visibility.Collapsed;

		return Visibility.Collapsed;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
