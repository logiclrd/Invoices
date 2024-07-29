namespace Invoices;

public enum PaymentType
{
	Unknown,

	Custom,

	Cash,
	eTransfer,
	WireTransfer,
	DebitCard,
	CreditCard,
	MasterCard,
	Visa,
	AmericanExpress,
	Discover,
	JCB,
	UnionPay,
}