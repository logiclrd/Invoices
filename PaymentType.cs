namespace Invoices;

public enum PaymentType
{
	Unknown,

	Custom,

	Cash,
	eTransfer,
	DebitCard,
	CreditCard,
	MasterCard,
	Visa,
	AmericanExpress,
	Discover,
	JCB,
	UnionPay,
}