using System;
using System.Collections.Generic;

namespace Invoices;

public class Invoice
{
	public int InvoiceNumber;
	public DateTime InvoiceDate;

	public List<string> Invoicee = new List<string>();

	public string PayableTo = "Wizards of the Plains";

	public string ProjectName = "";

	public DateTime? DueDate;

	public List<InvoiceItem> Items = new List<InvoiceItem>();

	public List<Tax> Taxes = new List<Tax>();
	public List<Payment> Payments = new List<Payment>();
}