using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Invoices.Interface.Converters;

public class CustomerIDValidityToVisibilityExtension : MarkupExtension, IValueConverter
{
	public override object ProvideValue(IServiceProvider serviceProvider)
	{
		return this;
	}
	
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		bool show =
			(value is int customerID) &&
			(customerID > 0);

		show ^= Invert;

		if (show)
			return Visibility.Visible;
		else
			return Visibility.Collapsed;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}

	public bool Invert { get; set; }
}
