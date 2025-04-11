using System;
using System.Linq;
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

		_allTaxes = _database.LoadTaxDefinitions().Values.ToArray();
	}

	TaxDefinition[] _allTaxes;

	void ilInvoices_InvoiceActivated(object? sender, Invoice invoice)
	{
		var ieInvoice = new InvoiceEditor();

		ieInvoice.HorizontalAlignment = HorizontalAlignment.Stretch;
		ieInvoice.VerticalAlignment = VerticalAlignment.Stretch;

		ieInvoice.LoadTaxes(_allTaxes);

		ieInvoice.Customers = _database.LoadCustomers().Values.ToList();

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

		ieInvoice.CreateOrUpdateCustomer +=
			(_, customer) =>
			{
				_database.SaveCustomer(customer);
			};

		void SaveInvoice()
		{
				try
				{
					_database.SaveInvoice(invoice);
				}
				catch (Exception e)
				{
					MessageBox.Show("Exception: " + e);
					return;
				}

				ithHeader.IsModified = false;
				ilInvoices.ReloadInvoice(invoice);
		}

		ieInvoice.Save += (_, _) => SaveInvoice();
		ithHeader.Save += (_, _) => ieInvoice.SaveInvoice();

		ithHeader.Close +=
			(_, _) =>
			{
				tcRoot.Items.Remove(tiTab);
				tcRoot.SelectedIndex = 0;
			};
	}
}