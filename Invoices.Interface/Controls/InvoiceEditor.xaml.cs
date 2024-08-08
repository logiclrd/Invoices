using System;
using System.Windows.Controls;

namespace Invoices.Interface.Controls;

using Invoices.Core;

public partial class InvoiceEditor : UserControl
{
	public InvoiceEditor()
	{
		InitializeComponent();
	}

	Invoice? _invoice;

	public Invoice? Invoice
	{
		get => _invoice;
		set
		{
			_invoice = value;

			// TODO
		}
	}

	public void Close()
	{
		Closed?.Invoke(this, EventArgs.Empty);
	}

	public event EventHandler? Closed;
}
