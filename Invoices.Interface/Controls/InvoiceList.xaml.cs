using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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

	public static readonly DependencyProperty InvoicesProperty = DependencyProperty.Register(nameof(Invoices), typeof(IList<Invoice>), typeof(InvoiceList));

	public IList<Invoice> Invoices
	{
		get => (IList<Invoice>)GetValue(InvoicesProperty);
		set
		{
			if (!(value is BindingList<Invoice>))
				value = new BindingList<Invoice>(value);

			SetValue(InvoicesProperty, value);
		}
	}

	public event EventHandler? SelectionChanged;
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

	void dgList_SelectedCellsChanged(object? sender, SelectedCellsChangedEventArgs e)
	{
		SelectionChanged?.Invoke(this, EventArgs.Empty);
	}

	public IEnumerable<Invoice> EnumerateSelectedInvoices()
	{
		return dgList.SelectedItems.OfType<Invoice>();
	}

	public void ReloadInvoice(Invoice invoice)
	{
		var invoices = this.Invoices;

		for (int i = 0; i < invoices.Count; i++)
			if (invoices[i].InvoiceID == invoice.InvoiceID)
			{
				invoices[i] = invoice;
				break;
			}
	}
}