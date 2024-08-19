using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Invoices.Interface.Converters;

using Invoices.Core;

public class CustomerSummaryExtension : MarkupExtension, IValueConverter
{
	public override object ProvideValue(IServiceProvider serviceProvider)
	{
		return this;
	}
	
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is Customer customer)
			return customer.Summary;

		return value?.ToString() ?? "";
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
