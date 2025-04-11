using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Invoices.Interface.Controls;

using Invoices.Core;
using Invoices.Interface.Utility;

public partial class InvoiceEditor : UserControl
{
	public InvoiceEditor()
	{
		InitializeComponent();

		PopulatePaymentTypes();

		dtpInvoiceDate.SelectedDate = dtpDueDate.SelectedDate = DateTime.Today;
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
	IList<Customer>? _customers;

	public Invoice? Invoice
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
					chkSetDueDate.IsChecked = value.DueDate.HasValue;
					dtpDueDate.SelectedDate = value.DueDate ?? DateTime.Today;
					txtCustomer.Text = value.InvoiceeCustomer?.LongSummary ?? "";
					cboState.SelectedValue = value.State;
					txtStateDescription.Text = value.StateDescription;
					dgItems.ItemsSource = value.Items;
					dgTaxes.ItemsSource = new BindingList<Tax>(value.Taxes);
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

	public IList<Customer>? Customers
	{
		get => _customers;
		set => _customers = value;
	}

	void txtInvoiceNumber_TextChanged(object? sender, TextChangedEventArgs e) => OnModified();
	void dtpInvoiceDate_SelectedDateChanged(object? sender, SelectionChangedEventArgs e) => OnModified();
	void chkSetDueDate_Checked(object? sender, RoutedEventArgs e) => OnModified();
	void chkSetDueDate_Unchecked(object? sender, RoutedEventArgs e) => OnModified();
	void dtpDueDate_SelectedDateChanged(object? sender, SelectionChangedEventArgs e) => OnModified();
	void cboState_SelectionChanged(object? sender, SelectionChangedEventArgs e) => OnModified();
	void txtStateDescription_TextChanged(object? sender, TextChangedEventArgs e) => OnModified();
	void txtNotes_TextChanged(object? sender, TextChangedEventArgs e) => OnModified();
	void txtInternalNotes_TextChanged(object? sender, TextChangedEventArgs e) => OnModified();
	void dgItems_CellEditEnding(object? sender, DataGridCellEditEndingEventArgs e) => OnModified();
	void dgPayments_CellEditEnding(object? sender, DataGridCellEditEndingEventArgs e) => OnModified();

	void dgTaxes_CellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
	{
		AnnealTaxes(e);
		OnModified();
	}

	void cmdChangeCustomer_Click(object? sender, RoutedEventArgs e)
	{
		if (_invoice is Invoice invoice)
		{
			var picker = new CustomerPicker();

			picker.Customers = _customers;

			if (invoice.InvoiceeCustomer != null)
				picker.SelectedCustomer = invoice.InvoiceeCustomer;

			picker.CreateOrUpdateSelectedCustomer += (_, _) => OnCreateUpdateCustomer(picker.SelectedCustomer ?? throw new Exception("Internal error: Received CreateOrUpdateSelectedCustomer event but SelectedCustomer is null."));

			picker.Owner = Window.GetWindow(this);

			bool customerSelected = picker.ShowDialog() ?? false;

			if (customerSelected)
			{
				invoice.InvoiceeCustomer = picker.SelectedCustomer;
				txtCustomer.Text = invoice.InvoiceeCustomer?.LongSummary ?? "";
				OnModified();
			}
		}
	}

	void AnnealTaxes(DataGridCellEditEndingEventArgs e)
	{
		if (FindResource("AllTaxes") is TaxDefinitionList tlAllTaxes)
		{
			if ((e.EditAction == DataGridEditAction.Commit)
			 && (e.Column == dgcbcTaxName))
			{
				var taxSelector = (ComboBox)e.EditingElement;

				int selectedTaxID = (int)taxSelector.SelectedValue;

				BindingList<Tax> gridSource = (BindingList<Tax>)dgTaxes.ItemsSource;

				for (int i=0; i < tlAllTaxes.Count; i++)
					if (tlAllTaxes[i].TaxID == selectedTaxID)
					{
						gridSource[e.Row.GetIndex()] = Tax.Rehydrate(tlAllTaxes[i]);
						break;
					}
			}
		}
	}

	void InvoiceEditor_PreviewKeyDown(object? sender, KeyEventArgs e)
	{
		if (((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) && (e.Key == Key.S))
		{
			e.Handled = true;

			SaveInvoice();
		}
	}

	public void SaveInvoice()
	{
		TransferChangesToModel();

		Save?.Invoke(this, EventArgs.Empty);
	}

	void TransferChangesToModel() => TransferChangesToModel(_invoice);

	void TransferChangesToModel(Invoice? model)
	{
		if (model == null)
			return;

		model.InvoiceNumber = txtInvoiceNumber.Text;
		model.InvoiceDate = dtpInvoiceDate.SelectedDate ?? DateTime.Today;

		if (chkSetDueDate.IsChecked ?? false)
			model.DueDate = dtpDueDate.SelectedDate ?? DateTime.Today;
		else
			model.DueDate = null;

		if (cboState.SelectedValue != null)
			model.State = (InvoiceState)cboState.SelectedValue;
		else
			model.State = InvoiceState.Ready;
		model.StateDescription = txtStateDescription.Text;

		model.Notes.Clear();
		model.Notes.AddRange(txtNotes.Text.Split('\n'));

		model.InternalNotes.Clear();
		model.InternalNotes.AddRange(txtInternalNotes.Text.Split('\n'));
	}

	bool _loading;

	void OnModified()
	{
		if (!_loading)
			Modified?.Invoke(this, EventArgs.Empty);
	}

	void OnCreateUpdateCustomer(Customer customer)
	{
		CreateOrUpdateCustomer?.Invoke(this, customer);

		if (customer.CustomerID < 0)
			throw new Exception("Internal error: Customer was not created when CreateCustomer event was fired by InvoiceEditor.");
	}

	public event EventHandler? Modified;
	public event EventHandler? Save;

	public event EventHandler<Customer>? CreateOrUpdateCustomer;

	void imgReceiptPrinter_MouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
	{
		if (_invoice != null)
		{
			var invoice = ObjectCloner.Clone(_invoice);

			TransferChangesToModel(invoice);

			var printPreview = new PrintPreview();

			printPreview.Owner = Window.GetWindow(this);
			printPreview.LoadInvoice(invoice);

			printPreview.ShowDialog();
		}
	}
}
