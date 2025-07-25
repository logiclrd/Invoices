using System;
using Square;

namespace Invoices.Integration.Square;

public static class MoneyExtensions
{
	public static decimal ToDecimal(this Money? money)
	{
		if (money == null)
			throw new NullReferenceException();

		if (!money.Amount.HasValue)
			throw new System.Exception("Money value is empty");
		if (!money.Currency.HasValue)
			throw new System.Exception("Currency value is empty");
		if (money.Currency.Value != Currency.Cad)
			throw new System.Exception("Currency is not CAD");

		return money.Amount.Value * 0.01M;
	}
}
