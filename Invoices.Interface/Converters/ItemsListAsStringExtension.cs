using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using System.Windows.Markup;

namespace Invoices.Interface.Converters;

using Invoices.Core;

public class ItemsListAsStringExtension : MarkupExtension, IValueConverter
{
	public override object ProvideValue(IServiceProvider serviceProvider)
	{
		return this;
	}
	
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is List<InvoiceItem> list)
		{
			var line = new StringBuilder();

			foreach (var item in list)
			{
				if (line.Length > 0)
					line.Append(" / ");

				if (item.Quantity != 1)
					line.Append(item.Quantity).Append(" x ");

				line.Append(item.Description);
			}

			return line.ToString();
		}

		return value?.ToString() ?? "";
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
