using System;

namespace Invoices.Core;

public class Payment
{
	public PaymentType PaymentType { get; set; }
	public string? PaymentTypeCustom { get; set; }
	public DateTime? ReceivedDateTime { get; set; }
	public decimal Amount { get; set; }
	public string? ReferenceNumber { get; set; }
	public decimal PaymentProcessingFee { get; set; }

	public string GetShortTypeDescription()
	{
		switch (PaymentType)
		{
			case PaymentType.Unknown: return "Unknown";

			case PaymentType.Custom: return PaymentTypeCustom!;

			case PaymentType.Cash: return "Cash";
			case PaymentType.eTransfer: return "e-Transfer";
			case PaymentType.WireTransfer: return "Wire";
			case PaymentType.PayPal: return "PayPal";
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

	public static Payment Rehydrate((int InvoiceID, int Sequence, PaymentType PaymentType, string? PaymentTypeCustom, DateTime? ReceivedDateTime, decimal Amount, string? ReferenceNumber, decimal PaymentProcessingFee) data)
		=> Rehydrate(data.PaymentType, data.PaymentTypeCustom, data.ReceivedDateTime, data.Amount, data.ReferenceNumber, data.PaymentProcessingFee);

	public static Payment Rehydrate(PaymentType paymentType, string? paymentTypeCustom, DateTime? receivedDateTime, decimal amount, string? referenceNumber, decimal paymentProcessingFee)
	{
		return
			new Payment()
			{
				PaymentType = paymentType,
				PaymentTypeCustom = paymentTypeCustom,
				ReceivedDateTime = receivedDateTime,
				Amount = amount,
				ReferenceNumber = referenceNumber,
				PaymentProcessingFee = paymentProcessingFee,
			};
	}

	public bool IsEmpty
	{
		get
		{
			return
				(PaymentType == default) &&
				string.IsNullOrWhiteSpace(PaymentTypeCustom) &&
				((ReceivedDateTime == null) || (ReceivedDateTime == default(DateTime))) &&
				(Amount == 0) &&
				string.IsNullOrWhiteSpace(ReferenceNumber) &&
				(PaymentProcessingFee == 0);
		}
	}
}
