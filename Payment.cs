using System;

namespace Invoices;

public class Payment
{
	public PaymentType PaymentType;
	public string? PaymentTypeCustom;
	public DateTime? ReceivedDateTime;
	public decimal Amount;

	public string GetShortTypeDescription()
	{
		switch (PaymentType)
		{
			case PaymentType.Unknown: return "Unknown";

			case PaymentType.Custom: return PaymentTypeCustom!;

			case PaymentType.Cash: return "Cash";
			case PaymentType.eTransfer: return "e-Transfer";
			case PaymentType.DebitCard: return "Debit";
			case PaymentType.CreditCard: return "Credit";
			case PaymentType.MasterCard: return "MasterCard";
			case PaymentType.Visa: return "Visa";
			case PaymentType.AmericanExpress: return "AmEx";
			case PaymentType.Discover: return "Discover";
			case PaymentType.JCB: return "JCB";
			case PaymentType.UnionPay: return "UnionPay";

			default: goto case PaymentType.Unknown;
		}
	}

	public static Payment Rehydrate((int InvoiceID, int Sequence, PaymentType PaymentType, string? PaymentTypeCustom, DateTime? ReceivedDateTime, decimal Amount) data)
		=> Rehydrate(data.PaymentType, data.PaymentTypeCustom, data.ReceivedDateTime, data.Amount);

	public static Payment Rehydrate(PaymentType paymentType, string? paymentTypeCustom, DateTime? receivedDateTime, decimal amount)
	{
		return
			new Payment()
			{
				PaymentType = paymentType,
				PaymentTypeCustom = paymentTypeCustom,
				ReceivedDateTime = receivedDateTime,
				Amount = amount,
			};
	}
}
