using System;
using System.Windows;
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

			if (value == null)
			{
				txtInvoiceNumber.Text = "";
				dtpInvoiceDate.SelectedDate = null;
				txtCustomer.Text = "";
				cboState.SelectedValue = null;
				txtStateDescription.Text = "";
				dgItems.ItemsSource = null;

				txtNotes.Text = "";
				txtInternalNotes.Text = "";
			}
			else
			{
				txtInvoiceNumber.Text = value.InvoiceNumber;
				dtpInvoiceDate.SelectedDate = value.InvoiceDate;
				txtCustomer.Text = value.InvoiceeCustomer?.LongSummary ?? "";
				cboState.SelectedValue = value.State;
				txtStateDescription.Text = value.StateDescription;
				dgItems.ItemsSource = value.Items;

				txtNotes.Text = string.Join("\n", value.Notes);
				txtInternalNotes.Text = string.Join("\n", value.InternalNotes);
			}
		}
	}

	void txtCustomer_DoubleClick(object? sender, RoutedEventArgs e)
	{
	}

	void dgItems_CellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
	{

	}

	public void Close()
	{
		Closed?.Invoke(this, EventArgs.Empty);
	}

	public event EventHandler? Closed;
}
