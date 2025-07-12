using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Markup;

namespace Invoices.Interface.Converters;

using Invoices.Core;
using Invoices.Interface.Utility;

public class InvoiceTotalExtension : MarkupExtension, IValueConverter
{
	public override object ProvideValue(IServiceProvider serviceProvider)
	{
		return this;
	}

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is Invoice invoice)
		{
			var taxes = invoice.Taxes.Sum(tax => tax.TaxRate);

			return invoice.Items.Sum(item => item.Quantity * item.UnitPrice * (taxes + 1.0M));
		}

		if (value.IsDataGridPlaceholder())
			return "";

		return value?.ToString() ?? "";
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
