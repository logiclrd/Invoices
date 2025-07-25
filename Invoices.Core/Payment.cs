using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Invoices.Core;

public class Payment : INotifyPropertyChanged
{
	PaymentType _paymentType;
	string? _paymentTypeCustom;
	DateTime? _receivedDateTimeUTC;
	decimal _amount;
	string? _referenceNumber;
	decimal _paymentProcessingFee;

	public PaymentType PaymentType
	{
		get => _paymentType;
		set { _paymentType = value; OnPropertyChanged(); }
	}

	public string? PaymentTypeCustom
	{
		get => _paymentTypeCustom;
		set { _paymentTypeCustom = value; OnPropertyChanged(); }
	}

	public DateTime? ReceivedDateTimeUTC
	{
		get => _receivedDateTimeUTC;
		set { _receivedDateTimeUTC = value; OnPropertyChanged(); OnPropertyChanged(nameof(ReceivedDate)); }
	}

	public DateTime? ReceivedDateTimeLocal
	{
		get
		{
			if (_receivedDateTimeUTC.HasValue)
				return _receivedDateTimeUTC.Value.ToLocalTime();
			else
				return null;
		}
		set
		{
			ReceivedDateTimeUTC = value?.ToUniversalTime();
		}
	}

	public DateTime? ReceivedDate
	{
		get
		{
			if (_receivedDateTimeUTC.HasValue)
				return _receivedDateTimeUTC.Value.ToLocalTime().Date;
			else
				return null;
		}
		set
		{
			_receivedDateTimeUTC = CalculateUpdatedReceivedDateTimeFromReceivedDateChange(_receivedDateTimeUTC?.ToLocalTime(), value)?.ToUniversalTime();
			OnPropertyChanged(nameof(ReceivedDateTimeUTC));
			OnPropertyChanged();
		}
	}

	public static DateTime? CalculateUpdatedReceivedDateTimeFromReceivedDateChange(DateTime? oldValue, DateTime? newDate)
	{
		return new DateTime(DateOnly.FromDateTime(newDate ?? DateTime.Today), TimeOnly.FromDateTime(oldValue ?? default));
	}

	public decimal Amount
	{
		get => _amount;
		set { _amount = value; OnPropertyChanged(); }
	}

	public string? ReferenceNumber
	{
		get => _referenceNumber;
		set { _referenceNumber = value; OnPropertyChanged(); }
	}

	public decimal PaymentProcessingFee
	{
		get => _paymentProcessingFee;
		set { _paymentProcessingFee = value; OnPropertyChanged(); }
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

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

	public static Payment Rehydrate((int InvoiceID, int Sequence, PaymentType PaymentType, string? PaymentTypeCustom, DateTime? ReceivedDateTimeUTC, decimal Amount, string? ReferenceNumber, decimal PaymentProcessingFee) data)
		=> Rehydrate(data.PaymentType, data.PaymentTypeCustom, data.ReceivedDateTimeUTC, data.Amount, data.ReferenceNumber, data.PaymentProcessingFee);

	public static Payment Rehydrate(PaymentType paymentType, string? paymentTypeCustom, DateTime? receivedDateTimeUTC, decimal amount, string? referenceNumber, decimal paymentProcessingFee)
	{
		return
			new Payment()
			{
				PaymentType = paymentType,
				PaymentTypeCustom = paymentTypeCustom,
				ReceivedDateTimeUTC = receivedDateTimeUTC,
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
				((ReceivedDateTimeUTC == null) || (ReceivedDateTimeUTC == default(DateTime))) &&
				(Amount == 0) &&
				string.IsNullOrWhiteSpace(ReferenceNumber) &&
				(PaymentProcessingFee == 0);
		}
	}
}
