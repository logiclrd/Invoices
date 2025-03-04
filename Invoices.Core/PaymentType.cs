using System.ComponentModel;

namespace Invoices.Core;

public enum PaymentType
{
	Unknown,

	Custom,

	Cash,
	[Description("e-Transfer")]
	eTransfer,
	[Description("Wire Transfer")]
	WireTransfer,
	PayPal,
	[Description("Debit Card")]
	DebitCard,
	[Description("Credit Card")]
	CreditCard,
	MasterCard,
	Visa,
	[Description("American Express")]
	AmericanExpress,
	Discover,
	JCB,
	UnionPay,
}