using System;
using System.Collections.Generic;

namespace Invoices.Core;

public class Invoice
{
	public int InvoiceID;

	public string InvoiceNumber { get; set; } = "";
	public DateTime InvoiceDate { get; set; }

	public InvoiceState State { get; set; }
	public string StateDescription { get; set; } = "";

	public List<string> Invoicee { get; set; } = new List<string>();

	public string PayableTo { get; set; } = "Wizards of the Plains";

	public string ProjectName { get; set; } = "";

	public DateTime? DueDate;

	public List<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();

	public List<Tax> Taxes { get; set; } = new List<Tax>();
	public List<Payment> Payments { get; set; } = new List<Payment>();

	public List<string> Notes { get; set; } = new List<string>();
	public List<string> InternalNotes { get; set; } = new List<string>();

	public List<int> PredecessorInvoiceIDs { get; set; } = new List<int>();
	public List<int> SuccessorInvoiceIDs { get; set; } = new List<int>();
	public List<int> RelatedInvoiceIDs { get; set; } = new List<int>();
}