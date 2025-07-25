namespace Invoices.Integration;

public enum ExternalPaymentStatus
{
	Invalid,

	Approved,
	Pending,
	Completed,
	Cancelled,
	Failed,
}
