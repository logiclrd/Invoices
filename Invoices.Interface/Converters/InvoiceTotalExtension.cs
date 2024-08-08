using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Markup;

namespace Invoices.Interface.Converters;

using Invoices.Core;

public class InvoiceTotalExtension : MarkupExtension, IValueConverter
{
	public override object ProvideValue(IServiceProvider serviceProvider)
	{
		return this;
	}
	
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is Invoice invoice)
			return invoice.Items.Sum(item => item.Quantity * item.UnitPrice).ToString("$#,##0.00");

		return value?.ToString() ?? "";
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
