using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

using Invoices.Core;

using Invoices.Integration;
using Invoices.Integration.Square;

using Invoices.Rendering;
using Invoices.Rendering.FullPage;
using Invoices.Rendering.Receipt;

namespace Invoices.Interface.Controls;

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
		var quickTaxes = new List<QuickItem>();

		if (FindResource("AllTaxes") is TaxDefinitionList tlAllTaxes)
			foreach (var tax in taxes)
			{
				tlAllTaxes.Add(tax);

				quickTaxes.Add(new QuickItem() { Label = tax.TaxName, Data = tax });
			}

		qipQuickTaxPicker.Items = quickTaxes;
	}

	public void LoadItemTemplates(IEnumerable<ItemTemplate> templates)
	{
		itpTemplatePicker.ItemTemplates = templates.ToList();
	}

	void PopulatePaymentTypes()
	{
		if (FindResource("AllPaymentTypes") is PaymentTypesList tlPaymentTypes)
		{
			var quickPayments = new List<QuickItem>();

			foreach (var paymentType in Enum.GetValues<PaymentType>())
			{
				if (paymentType == PaymentType.Unknown)
					continue;

				tlPaymentTypes.Add(paymentType);

				quickPayments.Add(new QuickItem() { Label = paymentType.ToString(), Data = paymentType });
			}

			qipQuickPaymentPicker.Items = quickPayments;
		}
	}

	public void SetNewInvoiceNumber(string invoiceNumber)
	{
		txtInvoiceNumber.Text = invoiceNumber;
		txtInvoiceNumber.Foreground = Brushes.Green;
	}

	public event EventHandler? InvoiceNumberChanged;

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
					txtItemTotal.Text = "";
					txtInvoiceTotal.Text = "";
				}
				else
				{
					var itemsBindingList = new BindingListEx<InvoiceItem>(value.Items);
					var taxesBindingList = new BindingListEx<Tax>(value.Taxes);
					var paymentsBindingList = new BindingListEx<Payment>(value.Payments);

					itemsBindingList.ListChanged +=
						(sender, e) =>
						{
							if (e.ListChangedType == ListChangedType.ItemDeleted)
								return;

							if ((e.ListChangedType == ListChangedType.ItemAdded)
							 && itemsBindingList[e.NewIndex].IsEmpty)
								return;

							OnModified();
							RecalculateItemTotal();
							RecalculateInvoiceTotal();
						};

					itemsBindingList.ItemRemoved +=
						(sender, item) =>
						{
							if (!item.IsEmpty)
							{
								OnModified();
								RecalculateItemTotal();
								RecalculateInvoiceTotal();
							}
						};

					taxesBindingList.ListChanged +=
						(sender, e) =>
						{
							if (e.ListChangedType == ListChangedType.ItemDeleted)
								return;

							if ((e.ListChangedType == ListChangedType.ItemAdded)
							 && taxesBindingList[e.NewIndex].IsEmpty)
								return;

							OnModified();
							RecalculateInvoiceTotal();
						};

					taxesBindingList.ItemRemoved +=
						(sender, item) =>
						{
							if (!item.IsEmpty)
							{
								OnModified();
								RecalculateInvoiceTotal();
							}
						};

					paymentsBindingList.ListChanged +=
						(sender, e) =>
						{
							if (e.ListChangedType == ListChangedType.ItemDeleted)
								return;

							if ((e.ListChangedType == ListChangedType.ItemAdded)
							 && paymentsBindingList[e.NewIndex].IsEmpty)
								return;

							OnModified();
						};

					paymentsBindingList.ItemRemoved +=
						(sender, item) =>
						{
							if (!item.IsEmpty)
								OnModified();
						};

					if (value.InvoiceDateUTC == default)
						value.InvoiceDateUTC = DateTime.Today;

					txtInvoiceNumber.Text = value.InvoiceNumber;
					dtpInvoiceDate.SelectedDate = value.InvoiceDateUTC;
					chkSetDueDate.IsChecked = value.DueDateUTC.HasValue;
					dtpDueDate.SelectedDate = value.DueDateUTC ?? DateTime.Today;
					txtCustomer.Text = value.InvoiceeCustomer?.LongSummary ?? "";
					cboState.SelectedValue = value.State;
					txtStateDescription.Text = value.StateDescription;
					dgItems.ItemsSource = itemsBindingList;
					dgTaxes.ItemsSource = taxesBindingList;
					dgPayments.ItemsSource = paymentsBindingList;

					txtNotes.Text = string.Join("\n", value.Notes);
					txtInternalNotes.Text = string.Join("\n", value.InternalNotes);

					SetTotalSpacerWidth();
					RecalculateItemTotal();
					RecalculateInvoiceTotal();
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

	void txtInvoiceNumber_TextChanged(object? sender, TextChangedEventArgs e) { OnModified(); txtInvoiceNumber.Foreground = Brushes.Black; }
	void dtpInvoiceDate_SelectedDateChanged(object? sender, SelectionChangedEventArgs e) => OnModified();
	void chkSetDueDate_Checked(object? sender, RoutedEventArgs e) => OnModified();
	void chkSetDueDate_Unchecked(object? sender, RoutedEventArgs e) => OnModified();
	void dtpDueDate_SelectedDateChanged(object? sender, SelectionChangedEventArgs e) => OnModified();
	void cboState_SelectionChanged(object? sender, SelectionChangedEventArgs e) => OnModified();
	void txtStateDescription_TextChanged(object? sender, TextChangedEventArgs e) => OnModified();
	void txtNotes_TextChanged(object? sender, TextChangedEventArgs e) => OnModified();
	void txtInternalNotes_TextChanged(object? sender, TextChangedEventArgs e) => OnModified();

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

	void itpTemplatePicker_ItemTemplateActivated(object? sender, ItemTemplate template)
	{
		var newItem = new InvoiceItem();

		newItem.Description = template.Description;
		newItem.Quantity = 1;
		var items = (BindingList<InvoiceItem>)dgItems.ItemsSource;

		newItem.UnitPrice = template.UnitPrice;

		items.Add(newItem);
	}

	void qipQuickTaxPicker_QuickItemActivated(object? sender, QuickItem item)
	{
		if (!(item.Data is TaxDefinition taxDefinition))
			return;
		if (_invoice == null)
			return;

		if (!(dgTaxes.ItemsSource is BindingList<Tax> taxesBindingList))
			return;

		var newTax = Tax.Rehydrate(taxDefinition);

		taxesBindingList.Add(newTax);

		OnModified();
	}

	bool _inRowEndingEvent = false;

	void dgItems_RowEditEnding(object? sender, DataGridRowEditEndingEventArgs e)
	{
		if (_inRowEndingEvent)
			return;

		_inRowEndingEvent = true;

		try
		{
			if (dgItems.SelectedItem is InvoiceItem item)
			{
				Dispatcher.BeginInvoke(
					DispatcherPriority.ApplicationIdle,
					() =>
					{
						if (item.IsEmpty)
						{
							if (!(dgItems.ItemsSource is BindingList<InvoiceItem> itemsBindingList))
								return;

							itemsBindingList.Remove(item);
						}
					});
			}
		}
		finally
		{
			_inRowEndingEvent = false;
		}
	}

	void dgTaxes_CellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
	{
		AnnealTaxes(e);
		OnModified();
	}

	void qipQuickPaymentPicker_QuickItemActivated(object? sender, QuickItem item)
	{
		if (!(item.Data is PaymentType paymentType))
			return;
		if (_invoice == null)
			return;

		if (!(dgPayments.ItemsSource is BindingList<Payment> paymentsBindingList))
			return;

		var newPayment = new Payment();

		newPayment.PaymentType = paymentType;
		newPayment.ReceivedDateTimeUTC = DateTime.UtcNow;
		newPayment.Amount = _lastCalculatedInvoiceTotal;

		paymentsBindingList.Add(newPayment);

		OnModified();
	}

	void ImportPayment(ExternalPayment externalPayment)
	{
		if (_invoice == null)
			return;
		if (!(dgPayments.ItemsSource is BindingList<Payment> paymentsBindingList))
			return;

		var newPayment = new Payment();

		newPayment.PaymentType = externalPayment.PaymentType;
		newPayment.ReceivedDateTimeUTC = externalPayment.TransactionDateTimeUTC;
		newPayment.ReferenceNumber = externalPayment.ReferenceNumber;
		newPayment.Amount = externalPayment.Total;
		newPayment.PaymentProcessingFee = externalPayment.ProcessingFee;

		paymentsBindingList.Add(newPayment);

		OnModified();
	}

	Window FindWindow()
	{
		DependencyObject obj = this;

		while (obj != null)
		{
			obj = VisualTreeHelper.GetParent(obj);

			if (obj is Window window)
				return window;
		}

		throw new Exception("Could not locate Window");
	}

	void ShowImportPaymentDialog(IPaymentDataSource dataSource)
	{
		var dialog = new PaymentImportDialog();

		dialog.Owner = FindWindow();
		dialog.SetDataSource(dataSource);

		bool? result = dialog.ShowDialog();

		if ((result ?? false) && (dialog.SelectedExternalPayment is ExternalPayment payment))
			ImportPayment(payment);
	}

	void cmdImportPayPal_Click(object? sender, RoutedEventArgs e)
	{
		MessageBox.Show("Not yet implemented");
	}

	void cmdImportSquare_Click(object? sender, RoutedEventArgs e)
	{
		string applicationPath = Path.GetDirectoryName(typeof(Program).Assembly.Location) ?? ".";

		string configPath = Path.Combine(
			applicationPath,
			"SquareConfiguration.json");

		var config = SquareConfiguration.LoadFrom(configPath);

		var dataSource = new SquareRecentPaymentDataSource(config);

		ShowImportPaymentDialog(dataSource);
	}

	void dgtcReceivedDateTime_DatePicker_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
	{
		if ((sender is DatePicker datePicker)
		 && (VisualTreeHelper.GetParent(datePicker) is DependencyObject editFrame))
		{
			for (int i = 0, l = VisualTreeHelper.GetChildrenCount(editFrame); i < l; i++)
			{
				if (VisualTreeHelper.GetChild(editFrame, i) is TextBox textBox)
				{
					textBox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
				}
			}
		}
	}

	void dgPayments_RowEditEnding(object? sender, DataGridRowEditEndingEventArgs e)
	{
		if (_inRowEndingEvent)
			return;

		_inRowEndingEvent = true;

		try
		{
			if (dgPayments.SelectedItem is Payment payment)
			{
				Dispatcher.BeginInvoke(
					DispatcherPriority.ApplicationIdle,
					() =>
					{
						if (payment.IsEmpty)
						{
							if (!(dgPayments.ItemsSource is BindingList<Payment> paymentsBindingList))
								return;

							paymentsBindingList.Remove(payment);
						}
					});
			}
		}
		finally
		{
			_inRowEndingEvent = false;
		}
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
			BindingList<Tax> gridSource = (BindingList<Tax>)dgTaxes.ItemsSource;

			if ((e.EditAction == DataGridEditAction.Commit)
			 && (e.Column == dgcbcTaxName))
			{
				var taxSelector = (ComboBox)e.EditingElement;

				if (taxSelector.SelectedValue is int selectedTaxID)
				{
					for (int i = 0; i < tlAllTaxes.Count; i++)
						if (tlAllTaxes[i].TaxID == selectedTaxID)
						{
							gridSource[e.Row.GetIndex()] = Tax.Rehydrate(tlAllTaxes[i]);
							break;
						}
				}
				else
				{
					e.Cancel = true;
					dgTaxes.CancelEdit();
				}
			}
		}
	}

	void RecalculateItemTotal()
	{
		if (_invoice == null)
			return;

		decimal total = 0.0M;

		foreach (var item in _invoice.Items)
			total += item.Quantity * item.UnitPrice;

		txtItemTotal.Text = total.ToString("$#,##0.00");

		_lastCalculatedItemTotal = total;
	}

	decimal _lastCalculatedItemTotal;

	void RecalculateInvoiceTotal()
	{
		if (_invoice == null)
			return;

		decimal taxes = 0.0M;

		foreach (var tax in _invoice.Taxes)
			taxes += tax.TaxRate;

		decimal taxesTotal = _lastCalculatedItemTotal * taxes;
		decimal total = _lastCalculatedItemTotal * (1.0M + taxes);

		txtInvoiceTaxesTotal.Text = taxesTotal.ToString("$#,##0.00");
		txtInvoiceTotal.Text = total.ToString("$#,##0.00");

		_lastCalculatedInvoiceTotal = total;
	}

	decimal _lastCalculatedInvoiceTotal;

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

	void dgItems_Cell_SizeChanged(object? sender, SizeChangedEventArgs e)
	{
		SetTotalSpacerWidth();
	}

	void SetTotalSpacerWidth()
	{
		double width = dgItems.RowHeaderWidth;

		foreach (var column in dgItems.Columns)
		{
			if (column == dgtcSubtotal)
				break;

			width += column.ActualWidth;
		}

		cItemTotalSpacer.Width = width - lblItemTotalHeader.ActualWidth;
		cInvoiceTotalSpacer.Width = width - lblInvoiceTotalHeader.ActualWidth;

		txtItemTotal.Width = dgtcSubtotal.ActualWidth;
		txtInvoiceTaxesTotal.Width = dgtcSubtotal.ActualWidth;
		txtInvoiceTotal.Width = dgtcSubtotal.ActualWidth;
	}

	void dgtcBackCalculateTax_Click(object? sender, RoutedEventArgs e)
	{
		if (FindGridCellRootElements(e.Source) is (DataGridRow row, DataGridCell cell))
		{
			if (!(row.Item is InvoiceItem item) || (_invoice == null))
			{
				SystemSounds.Asterisk.Play();
				return;
			}

			decimal totalTaxRate = 0.0M;

			foreach (var tax in _invoice.Taxes)
				totalTaxRate += tax.TaxRate;

			item.UnitPrice /= (1.0M + totalTaxRate);
		}
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

		InvoiceNumberChanged?.Invoke(this, EventArgs.Empty);

		txtInvoiceNumber.Foreground = Brushes.Black;

		_modified = false;
	}

	void TransferChangesToModel() => TransferChangesToModel(_invoice);

	void TransferChangesToModel(Invoice? model)
	{
		if (model == null)
			return;

		model.InvoiceNumber = txtInvoiceNumber.Text;
		model.InvoiceDateUTC = dtpInvoiceDate.SelectedDate ?? DateTime.Today;

		if (chkSetDueDate.IsChecked ?? false)
			model.DueDateUTC = dtpDueDate.SelectedDate?.ToUniversalTime() ?? DateTime.Today;
		else
			model.DueDateUTC = null;

		if (cboState.SelectedValue != null)
			model.State = (InvoiceState)cboState.SelectedValue;
		else
			model.State = InvoiceState.Ready;
		model.StateDescription = txtStateDescription.Text;

		model.Notes.Clear();
		model.Notes.AddRange(txtNotes.Text.Split('\n'));

		model.InternalNotes.Clear();
		model.InternalNotes.AddRange(txtInternalNotes.Text.Split('\n'));

		foreach (var payment in model.Payments)
			if (payment.ReceivedDateTimeUTC == null)
				payment.ReceivedDateTimeUTC = DateTime.UtcNow;
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

	void ShowPrintPreview<TRenderer>()
		where TRenderer : InvoiceRenderer, new()
	{
		if (_invoice != null)
		{
			var invoice = ObjectCloner.Clone(_invoice);

			TransferChangesToModel(invoice);

			var renderer = new TRenderer();

			PrintUtility.Print(FindWindow(), renderer, invoice);
		}
	}

	void imgReceiptPrinter_MouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
	{
		ShowPrintPreview<ReceiptInvoiceRenderer>();
	}

	void imgFullPagePrinter_MouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
	{
		ShowPrintPreview<FullPageInvoiceRenderer>();
	}
}
