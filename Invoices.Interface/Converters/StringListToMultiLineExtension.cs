using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Markup;

namespace Invoices.Interface.Converters;

public class StringListToMultiLineExtension : MarkupExtension, IValueConverter
{
	public override object ProvideValue(IServiceProvider serviceProvider)
	{
		return this;
	}
	
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is IList<string> list)
			return string.Join("\n", list);

		return value?.ToString() ?? "";
	}

	static char[] NewlineCharacters = { '\r', '\n' };

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is string sequence)
			return sequence.Split(NewlineCharacters, StringSplitOptions.RemoveEmptyEntries).ToList();

		return Array.Empty<string>();
	}
}
