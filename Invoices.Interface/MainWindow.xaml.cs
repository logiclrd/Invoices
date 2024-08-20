using System.Windows;
using System.Windows.Controls;

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
		var ieInvoice = new InvoiceEditor();

		ieInvoice.HorizontalAlignment = HorizontalAlignment.Stretch;
		ieInvoice.VerticalAlignment = VerticalAlignment.Stretch;

		ieInvoice.Invoice = invoice;

		var ithHeader = new InvoiceTabHeader();

		ithHeader.Title = "Invoice #" + invoice.InvoiceNumber;

		var tiTab = new TabItem();

		tiTab.Content = ieInvoice;
		tiTab.Header = ithHeader;

		tcRoot.Items.Add(tiTab);
		tcRoot.SelectedItem = tiTab;

		ieInvoice.Modified +=
			(_, _) =>
			{
				ithHeader.IsModified = true;
			};

		ieInvoice.Save +=
			(_, _) =>
			{
				ilInvoices.ReloadInvoice(invoice.InvoiceID);
			};

		ithHeader.Close +=
			(_, _) =>
			{
				tcRoot.Items.Remove(tiTab);
				tcRoot.SelectedIndex = 0;
			};
	}
}