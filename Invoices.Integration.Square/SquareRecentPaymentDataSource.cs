using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Xml;
using Invoices.Core;

namespace Invoices.Integration.Square;

public class SquareRecentPaymentDataSource : IPaymentDataSource
{
	public string Name => "Square";

	global::Square.SquareClient? _client;

	SquareConfiguration _config;

	public SquareRecentPaymentDataSource(SquareConfiguration config)
	{
		_config = config;
		_client = new global::Square.SquareClient(
			config.AccessToken,
			new global::Square.ClientOptions()
			{
				BaseUrl = config.EndPoint ?? throw new Exception("Missing configuration: " + nameof(config.EndPoint)),
			});
	}

	public async IAsyncEnumerable<ExternalPayment> GetRecentPayments(TimeSpan window)
	{
		if (_client == null)
			throw new Exception("Not initialized");

		global::Square.Payments.ListPaymentsRequest request =
			new global::Square.Payments.ListPaymentsRequest()
			{
				BeginTime = XmlConvert.ToString(DateTime.UtcNow - window, XmlDateTimeSerializationMode.Utc),
			};

		global::Square.RequestOptions options = new global::Square.RequestOptions();

		var pager = await _client.Payments.ListAsync(request, options, CancellationToken.None);

		await foreach (var item in pager)
		{
			if (item == null)
				continue;
			if ((item.Status != "APPROVED") && (item.Status != "COMPLETED"))
				continue;

			string? referenceNumber = ExtractReferenceNumber(item);

			var payment = new ExternalPayment();

			payment.TransactionDateTimeUTC = (item.CreatedAt == null) ? null : DateTime.Parse(item.CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
			payment.Amount = item.AmountMoney.ToDecimal();
			payment.Total = item.TotalMoney.ToDecimal(); ;

			if (item.ProcessingFee != null)
				payment.ProcessingFee = item.ProcessingFee.Sum(fee => fee.AmountMoney.ToDecimal());

			payment.SourceType = item.SourceType;
			payment.BankName = item.BankAccountDetails?.BankName ?? "";
			payment.BuyerEmailAddress = item.BuyerEmailAddress;
			payment.CardBrand = item.CardDetails?.Card?.CardBrand?.Value ?? "";
			payment.AuthResultCode = item.CardDetails?.AuthResultCode;
			payment.ReferenceNumber = referenceNumber;
			payment.PaymentStatus = item.Status == null ? ExternalPaymentStatus.Invalid : Enum.Parse<ExternalPaymentStatus>(item.Status, ignoreCase: true);

			payment.PaymentType = InferPaymentType(item);

			yield return payment;
		}
	}

	string ExtractReferenceNumber(global::Square.Payment payment)
	{
		if (payment.CardDetails != null)
		{
			if (payment.CardDetails.AuthResultCode != null)
				return payment.CardDetails.AuthResultCode;
		}

		if (payment.ExternalDetails != null)
		{
			if (payment.ExternalDetails.SourceId != null)
				return payment.ExternalDetails.SourceId;
		}

		return payment.ReferenceId ?? payment.Id ?? "";
	}

	PaymentType InferPaymentType(global::Square.Payment payment)
	{
		string? cardBrand = payment.CardDetails?.Card?.CardBrand?.Value;

		if ((cardBrand != null) && Enum.TryParse<PaymentType>(cardBrand, ignoreCase: true, out var paymentTypeFromCardBrand))
			return paymentTypeFromCardBrand;

		if (payment.SourceType != null)
		{
			switch (payment.SourceType)
			{
				case "CARD": return PaymentType.CreditCard;
				case "BANK_ACCOUNT": return PaymentType.DebitCard;
				case "CASH": return PaymentType.Cash;
				default: return PaymentType.IntermediateWallet;
			}
		}

		return PaymentType.Unknown;
	}
}
