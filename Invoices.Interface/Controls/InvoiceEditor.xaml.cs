using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Invoices.Interface.Controls;

using System.ComponentModel;
using System.Reflection;
using Invoices.Core;

public partial class InvoiceEditor : UserControl
{
	public InvoiceEditor()
	{
		InitializeComponent();

		PopulatePaymentTypes();
	}

	public void LoadTaxes(IEnumerable<TaxDefinition> taxes)
	{
		if (FindResource("AllTaxes") is TaxDefinitionList tlAllTaxes)
			foreach (var tax in taxes)
				tlAllTaxes.Add(tax);
	}

	void PopulatePaymentTypes()
	{
		if (FindResource("AllPaymentTypes") is PaymentTypesList tlPaymentTypes)
		{
			foreach (var paymentType in Enum.GetValues<PaymentType>())
			{
				if (paymentType == PaymentType.Unknown)
					continue;

				tlPaymentTypes.Add(paymentType);
			}
		}
	}

	Invoice? _invoice;

	public Invoice? 	Invoice
	{
		get => _invoice;
		set
		{
			_invoice = value;

			_loading = true;

			try
			{
				if (value == null)
				{
					txtInvoiceNumber.Text = "";
					dtpInvoiceDate.SelectedDate = null;
					txtCustomer.Text = "";
					cboState.SelectedValue = null;
					txtStateDescription.Text = "";
					dgItems.ItemsSource = null;
					dgTaxes.ItemsSource = null;
					dgPayments.ItemsSource = null;

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
					dgTaxes.ItemsSource = value.Taxes;
					dgPayments.ItemsSource = value.Payments;

					txtNotes.Text = string.Join("\n", value.Notes);
					txtInternalNotes.Text = string.Join("\n", value.InternalNotes);
				}
			}
			finally
			{
				_loading = false;
			}
		}
	}

	void txtInvoiceNumber_TextChanged(object? sender, TextChangedEventArgs e) => OnModified();
	void dtpInvoiceDate_SelectedDateChanged(object? sender, SelectionChangedEventArgs e) => OnModified();
	void cboState_SelectionChanged(object? sender, SelectionChangedEventArgs e) => OnModified();
	void txtStateDescription_TextChanged(object? sender, TextChangedEventArgs e) => OnModified();
	void txtNotes_TextChanged(object? sender, TextChangedEventArgs e) => OnModified();
	void txtInternalNotes_TextChanged(object? sender, TextChangedEventArgs e) => OnModified();
	void dgTaxes_CellEditEnding(object? sender, DataGridCellEditEndingEventArgs e) => OnModified();
	void dgItems_CellEditEnding(object? sender, DataGridCellEditEndingEventArgs e) => OnModified();
	void dgPayments_CellEditEnding(object? sender, DataGridCellEditEndingEventArgs e) => OnModified();

	void cmdAddTax_Click(object? sender, EventArgs e) => MessageBox.Show("TODO");
	void cmdRemoveTax_Click(object? sender, EventArgs e) => MessageBox.Show("TODO");

	void InvoiceEditor_PreviewKeyDown(object? sender, KeyEventArgs e)
	{
		if (((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) && (e.Key == Key.S))
		{
			e.Handled = true;

			TransferChangesToModel();

			Save?.Invoke(this, EventArgs.Empty);
		}
	}

	void TransferChangesToModel()
	{
		if (_invoice == null)
			return;

		_invoice.InvoiceNumber = txtInvoiceNumber.Text;
		_invoice.InvoiceDate = dtpInvoiceDate.SelectedDate ?? DateTime.MinValue;
		_invoice.State = (InvoiceState)cboState.SelectedValue;
		_invoice.StateDescription = txtStateDescription.Text;

		_invoice.Notes.Clear();
		_invoice.Notes.AddRange(txtNotes.Text.Split('\n'));

		_invoice.InternalNotes.Clear();
		_invoice.InternalNotes.AddRange(txtInternalNotes.Text.Split('\n'));
	}

	bool _loading;

	void OnModified()
	{
		if (!_loading)
			Modified?.Invoke(this, EventArgs.Empty);
	}

	public event EventHandler? Modified;
	public event EventHandler? Save;
}
