using System.ComponentModel;

namespace Invoices.Core;

public enum InvoiceState
{
	Unknown,

	[Description("Ready for me to do work on")]
	Ready,
	[Description("Waiting on something external")]
	Waiting,
	[Description("Completed")]
	Finished,
}