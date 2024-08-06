using System;
using System.Collections.Generic;

namespace Invoices.Core;

public class Invoice
{
	public int InvoiceID;

	public string InvoiceNumber = "";
	public DateTime InvoiceDate;

	public InvoiceState State;
	public string StateDescription = "";

	public List<string> Invoicee = new List<string>();

	public string PayableTo = "Wizards of the Plains";

	public string ProjectName = "";

	public DateTime? DueDate;

	public List<InvoiceItem> Items = new List<InvoiceItem>();

	public List<Tax> Taxes = new List<Tax>();
	public List<Payment> Payments = new List<Payment>();

	public List<string> Notes = new List<string>();
	public List<string> InternalNotes = new List<string>();

	public List<int> PredecessorInvoiceIDs = new List<int>();
	public List<int> SuccessorInvoiceIDs = new List<int>();
	public List<int> RelatedInvoiceIDs = new List<int>();
}