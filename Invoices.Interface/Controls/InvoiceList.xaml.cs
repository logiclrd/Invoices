using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Invoices.Interface.Controls;

using Invoices.Core;

public partial class InvoiceList : UserControl
{
	public InvoiceList()
	{
		InitializeComponent();

		DataContext = this;
	}

	public static DependencyProperty InvoicesProperty = DependencyProperty.Register(nameof(Invoices), typeof(IEnumerable<Invoice>), typeof(InvoiceList));

	public IEnumerable<Invoice> Invoices
	{
		get => (IEnumerable<Invoice>)GetValue(InvoicesProperty);
		set => SetValue(InvoicesProperty, value);
	}

	public event EventHandler<Invoice>? InvoiceActivated;

	void dgList_LoadingRow(object? sender, DataGridRowEventArgs e)
	{
		e.Row.MouseDoubleClick +=
			(_, innerE) =>
			{
				var invoice = e.Row.DataContext as Invoice;

				if (invoice != null)
				{
					innerE.Handled = true;
					InvoiceActivated?.Invoke(this, invoice);
				}
			};
	}

	public void ReloadInvoice(int invoiceID)
	{
		// TODO
	}
}