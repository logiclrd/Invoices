using System.Windows;

namespace Invoices.Interface;

using Invoices.Core;

using Invoices.Interface.Controls;

public partial class MainWindow : Window
{
	Database _database;

	public MainWindow()
	{
		InitializeComponent();

		_database = new Database();

		ilInvoices.Invoices = _database.LoadInvoices();
	}

	void ilInvoices_InvoiceActivated(object? sender, Invoice invoice)
	{
		ilInvoices.IsEnabled = false;
		ilInvoices.Visibility = Visibility.Collapsed;

		var ieInvoice = new InvoiceEditor();

		ieInvoice.HorizontalAlignment = HorizontalAlignment.Stretch;
		ieInvoice.VerticalAlignment = VerticalAlignment.Stretch;

		ieInvoice.Invoice = invoice;

		grdRoot.Children.Add(ieInvoice);

		ieInvoice.Closed +=
			(_, _) =>
			{
				grdRoot.Children.Remove(ieInvoice);
				ilInvoices.Visibility = Visibility.Visible;
				ilInvoices.IsEnabled = true;
				ilInvoices.ReloadInvoice(invoice.InvoiceID);
			};
	}
}