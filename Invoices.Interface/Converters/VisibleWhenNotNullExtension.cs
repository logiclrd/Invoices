using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Invoices.Interface.Converters;

using System.Windows;
using System.Windows.Controls;

public class VisibleWhenNotNullExtension : MarkupExtension, IValueConverter
{
	public override object ProvideValue(IServiceProvider serviceProvider)
	{
		return this;
	}
	
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if ((value != null) && (value.GetType().Name != "NamedObject")) // DataGrid.NewItemPlaceholder is a NamedObject. For some reason, neither DataGrid.NewItemPlaceholder nor NamedObject are public. Sigh.
			return Visibility.Visible;
		else
			return Visibility.Collapsed;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
