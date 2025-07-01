using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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

	public void LoadItemTemplates(IEnumerable<ItemTemplate> templates)
	{
		itpTemplatePicker.ItemTemplates = templates.ToList();
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
					var itemsBindingList = new BindingList<InvoiceItem>(value.Items);
					var taxesBindingList = new BindingList<Tax>(value.Taxes);

					itemsBindingList.ListChanged += (_, _) => OnModified();
					taxesBindingList.ListChanged += (_, _) => OnModified();

					txtInvoiceNumber.Text = value.InvoiceNumber;
					dtpInvoiceDate.SelectedDate = value.InvoiceDate;
					chkSetDueDate.IsChecked = value.DueDate.HasValue;
					dtpDueDate.SelectedDate = value.DueDate ?? DateTime.Today;
					txtCustomer.Text = value.InvoiceeCustomer?.LongSummary ?? "";
					cboState.SelectedValue = value.State;
					txtStateDescription.Text = value.StateDescription;
					dgItems.ItemsSource = itemsBindingList;
					dgTaxes.ItemsSource = taxesBindingList;
					dgPayments.ItemsSource = value.Payments;

					txtNotes.Text = string.Join("\n", value.Notes);
					txtInternalNotes.Text = string.Join("\n", value.InternalNotes);
				}

				_modified = false;
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
	void dgItems_RowEditEnding(object? sender, DataGridRowEditEndingEventArgs e) => OnModified();

	void dtpInvoiceDate_PreviewKeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key == Key.Home)
		{
			e.Handled = true;
			dtpInvoiceDate.SelectedDate = DateTime.Today;
			OnModified();
		}
	}

	void dtpDueDate_PreviewKeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key == Key.Home)
		{
			e.Handled = true;
			dtpDueDate.SelectedDate = DateTime.Today;
			OnModified();
		}
	}

	void tbTemplates_SizeChanged(object? sender, SizeChangedEventArgs e)
	{
		pTemplatePicker.Width = e.NewSize.Width;
	}

	void itpTemplatePicker_ItemTemplateActivated(object? sender, ItemTemplate template)
	{
		var newItem = new InvoiceItem();

		newItem.Description = template.Description;
		newItem.Quantity = 1;
		var items = (BindingList<InvoiceItem>)dgItems.ItemsSource;

		newItem.UnitPrice = template.UnitPrice;

		items.Add(newItem);

		tbTemplates.IsChecked = false;
	}

	void itpTemplatePicker_LostFocus(object? sender, RoutedEventArgs e)
	{
		if (e.OriginalSource == itpTemplatePicker)
			tbTemplates.IsChecked = false;
	}

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

	(DataGridRow Row, DataGridCell Cell)? FindGridCellRootElements(object eventSource)
	{
		var trace = eventSource as DependencyObject;

		while (trace != null)
		{
			if (trace is DataGridCell cell)
			{
				while (trace != null)
				{
					if (trace is DataGridRow row)
						return (row, cell);

					trace = VisualTreeHelper.GetParent(trace);
				}
			}

			trace = VisualTreeHelper.GetParent(trace);
		}

		return null;
	}

	void dgPayments_CellPreviewMouseDown(object? sender, MouseButtonEventArgs e)
	{
		if (FindGridCellRootElements(e.Source) is (DataGridRow row, DataGridCell cell))
		{
			if (cell.Column == dgtcReferenceNumber)
			{
				if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && (e.ChangedButton == MouseButton.Left))
				{
					if (!(row.Item is Payment payment))
					{
						SystemSounds.Asterisk.Play();
						return;
					}

					switch (payment.PaymentType)
					{
						case PaymentType.PayPal:
						{
							string referenceNumber = payment.ReferenceNumber?.Trim() ?? "";

							if (string.IsNullOrEmpty(referenceNumber))
							{
								SystemSounds.Asterisk.Play();
								break;
							}

							string paypalTransactionUri = $"https://www.paypal.com/myaccount/activities/details/{referenceNumber}";

							ActivateUri?.Invoke(this, new Uri(paypalTransactionUri));

							break;
						}

						default:
							SystemSounds.Asterisk.Play();
							break;
					}
				}
			}
		}
	}

	public bool PromptSaveInvoice()
	{
		if (_modified)
		{
			var result = MessageBox.Show("Save changes?", "Modified", MessageBoxButton.YesNoCancel);

			if (result == MessageBoxResult.Yes)
				SaveInvoice();
			if (result == MessageBoxResult.Cancel)
				return false;
		}

		return true;
	}

	void InvoiceEditor_PreviewKeyDown(object? sender, KeyEventArgs e)
	{
		if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && (e.Key == Key.S))
		{
			e.Handled = true;

			SaveInvoice();
		}

		if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && ((e.Key == Key.W) || (e.Key == Key.F4)))
		{
			e.Handled = true;

			if (!PromptSaveInvoice())
				return;

			OnClose();
		}
	}

	public void SaveInvoice()
	{
		TransferChangesToModel();

		Save?.Invoke(this, EventArgs.Empty);

		_modified = false;
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
	bool _modified;

	public bool IsModified => _modified;

	void OnModified()
	{
		if (!_loading)
		{
			_modified = true;
			Modified?.Invoke(this, EventArgs.Empty);
		}
	}

	void OnCreateUpdateCustomer(Customer customer)
	{
		CreateOrUpdateCustomer?.Invoke(this, customer);

		if (customer.CustomerID < 0)
			throw new Exception("Internal error: Customer was not created when CreateCustomer event was fired by InvoiceEditor.");
	}

	void OnClose()
	{
		Close?.Invoke(this, EventArgs.Empty);
	}

	public event EventHandler? Modified;
	public event EventHandler? Save;
	public event EventHandler? Close;

	public event EventHandler<Customer>? CreateOrUpdateCustomer;
	public event EventHandler<Uri>? ActivateUri;

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
