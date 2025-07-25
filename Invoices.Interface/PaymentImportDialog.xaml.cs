using System.Threading.Tasks;
using System.Windows;

namespace Invoices.Interface;

using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Invoices.Integration;

public partial class PaymentImportDialog : Window
{
	public PaymentImportDialog()
	{
		InitializeComponent();

		_listBoxData = new BindingList<ExternalPayment>();

		lbRecentPayments.ItemsSource = _listBoxData;
	}

	IPaymentDataSource? _paymentDataSource;
	BindingList<ExternalPayment> _listBoxData;

	public void SetDataSource(IPaymentDataSource paymentDataSource)
	{
		_paymentDataSource = paymentDataSource;

		Title = "Import Payment from Processor: " + paymentDataSource.Name;

		StartLoadPayments();
	}

	public ExternalPayment? SelectedExternalPayment
	{
		get => lbRecentPayments.SelectedItem as ExternalPayment;
	}

	void txtSeconds_GotFocus(object? sender, RoutedEventArgs e)
	{
		cmdRefresh.IsDefault = true;
	}

	void txtSeconds_LostFocus(object? sender, RoutedEventArgs e)
	{
		cmdRefresh.IsDefault = false;
	}

	void cmdRefresh_Click(object? sender, RoutedEventArgs e)
	{
		StartLoadPayments();
		lbRecentPayments.Focus();
	}

	void StartLoadPayments()
	{
		if (!int.TryParse(txtSeconds.Text, out var windowSeconds))
		{
			txtSeconds.Text = "900";
			windowSeconds = 900;
		}

		var window = TimeSpan.FromSeconds(windowSeconds);

		cmdRefresh.IsEnabled = false;
		_listBoxData.Clear();

		Task.Run(() => LoadPayments(window));
	}

	async Task LoadPayments(TimeSpan window)
	{
		UI(() => lblRefreshing.Visibility = Visibility.Visible);

		if (_paymentDataSource != null)
		{
			await foreach (var payment in _paymentDataSource.GetRecentPayments(window))
			{
				UI(() =>
					{
						lblRefreshing.Visibility = Visibility.Collapsed;
						_listBoxData.Add(payment);
					});
			}
		}

		UI(
			() =>
			{
				lblRefreshing.Visibility = Visibility.Collapsed; // In case there were no results
				cmdRefresh.IsEnabled = true;
			});
	}

	void lbRecentPayments_GotFocus(object? sender, RoutedEventArgs e)
	{
		cmdImport.IsDefault = true;
	}

	void lbRecentPayments_LostFocus(object? sender, RoutedEventArgs e)
	{
		cmdImport.IsDefault = false;
	}

	void lbRecentPayments_ItemDoubleClick(object? sender, MouseEventArgs e)
	{
		if ((e.Source is Control control)
		 && (control.DataContext is ExternalPayment payment))
		{
			lbRecentPayments.SelectedItem = payment;
			DialogResult = true;
		}
	}

	void cmdImport_Click(object? sender, RoutedEventArgs e)
	{
		DialogResult = true;
	}

	void cmdCancel_Click(object? sender, RoutedEventArgs e)
	{
		DialogResult = false;
	}

	void UI(Action action)
	{
		if (!Dispatcher.CheckAccess())
		{
			Dispatcher.Invoke(action, DispatcherPriority.Send);
			return;
		}

		action();
	}
}
