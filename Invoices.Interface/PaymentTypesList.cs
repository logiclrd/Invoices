using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;
using System.Windows.Markup;

namespace Invoices.Interface;

using System.Collections.Generic;
using Invoices.Core;

public class PaymentTypesList : BindingList<PaymentType>
{
	static Dictionary<PaymentType, string> s_descriptionForValue;
	static Dictionary<string, PaymentType> s_valueForDescription;

	static PaymentTypesList()
	{
		s_descriptionForValue = new Dictionary<PaymentType, string>();
		s_valueForDescription = new Dictionary<string, PaymentType>();

		foreach (var paymentType in Enum.GetValues<PaymentType>())
		{
			if (paymentType != PaymentType.Unknown)
			{
				var description = ExtractDescription(paymentType);

				s_descriptionForValue[paymentType] = description;
				s_valueForDescription[description] = paymentType;
			}
		}
	}

	public static string ExtractDescription(PaymentType paymentType)
	{
		var description = paymentType.ToString();

		if ((typeof(PaymentType).GetField(description) is FieldInfo fieldInfo)
			&& (Attribute.GetCustomAttribute(fieldInfo, typeof(DescriptionAttribute)) is DescriptionAttribute attribute))
			description = attribute.Description;

		return description;
	}

	public class Converter : MarkupExtension, IValueConverter
	{
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if ((value is PaymentType paymentType) && (targetType == typeof(string)))
			{
				if (!s_descriptionForValue.TryGetValue(paymentType, out var matchingDescription))
					matchingDescription = paymentType.ToString();

				return matchingDescription;
			}

			if ((value is string description) && (targetType == typeof(PaymentType)))
			{
				if (!s_valueForDescription.TryGetValue(description, out var matchingPaymentType))
					matchingPaymentType = PaymentType.Unknown;

				return matchingPaymentType;
			}

			return value;
		}
		
		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> Convert(value, targetType, parameter, culture);

		public override object ProvideValue(IServiceProvider serviceProvider) => this;
	}
}
