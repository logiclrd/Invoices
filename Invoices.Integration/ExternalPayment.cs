using System;
using Invoices.Core;

namespace Invoices.Integration;

public class ExternalPayment
{
	public DateTime? TransactionDateTimeUTC { get; set; }
	public decimal Amount { get; set; }
	public decimal Total { get; set; }
	public decimal ProcessingFee { get; set; }
	public string? SourceType { get; set; }
	public string? BankName { get; set; }
	public string? BuyerEmailAddress { get; set; }
	public string? CardBrand { get; set; }
	public string? AuthResultCode { get; set; }
	public string? ReferenceNumber { get; set; }
	public PaymentType PaymentType { get; set; }
	public ExternalPaymentStatus PaymentStatus { get; set; }

	public DateTime? TransactionDateTime => TransactionDateTimeUTC?.ToLocalTime();
}