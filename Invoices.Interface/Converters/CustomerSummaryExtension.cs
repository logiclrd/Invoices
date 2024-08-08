using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Markup;

namespace Invoices.Interface.Converters;

using System.Text;
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
		{
			var result = new StringBuilder();

			foreach (string name in customer.Name)
			{
				if (result.Length > 0)
					result.Append(", ");
				result.Append(name);
			}

			if (customer.Address.Any())
				result.Append(" (").Append(customer.Address[0]).Append(")");
			else if (customer.EmailAddresses.Any() || customer.PhoneNumbers.Any())
				result.Append(" (").Append(string.Join(", ", customer.EmailAddresses.Concat(customer.PhoneNumbers))).Append(")");

			return result.ToString();
		}

		return value?.ToString() ?? "";
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
