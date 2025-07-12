using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Markup;

namespace Invoices.Interface.Converters;

using Invoices.Core;
using Invoices.Interface.Utility;

public class InvoiceOutstandingExtension : MarkupExtension, IValueConverter
{
	public override object ProvideValue(IServiceProvider serviceProvider)
	{
		return this;
	}

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is Invoice invoice)
		{
			decimal taxFactor = 1.0M;

			foreach (var tax in invoice.Taxes)
				taxFactor += tax.TaxRate;

			return invoice.Items.Sum(item => item.Quantity * item.UnitPrice) * taxFactor - invoice.Payments.Sum(payment => payment.Amount);
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
