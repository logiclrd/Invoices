namespace Invoices;

public enum PaymentType
{
	Unknown,

	Custom,

	Cash,
	eTransfer,
	WireTransfer,
	PayPal,
	DebitCard,
	CreditCard,
	MasterCard,
	Visa,
	AmericanExpress,
	Discover,
	JCB,
	UnionPay,
}