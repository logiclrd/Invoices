using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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

		ieInvoice.LoadItemTemplates(_database.LoadItemTemplates());
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

		ieInvoice.ActivateUri +=
			(_, uri) =>
			{
				OpenUrl(uri.ToString());
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

		void CloseTab()
		{
			tcRoot.Items.Remove(tiTab);
			tcRoot.SelectedIndex = 0;
		}

		ieInvoice.Save += (_, _) => SaveInvoice();
		ithHeader.Save += (_, _) => ieInvoice.SaveInvoice();

		ieInvoice.Close +=
			(_, _) =>
			{
				CloseTab();
			};

		ithHeader.Close += (_, _) => CloseTab();
	}

	private void OpenUrl(string url)
	{
		try
		{
			Process.Start(url);
		}
		catch
		{
			// hack because of this: https://github.com/dotnet/corefx/issues/10361
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				url = url.Replace("&", "^&");
				Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				Process.Start("xdg-open", url);
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
				Process.Start("open", url);
			else
				throw;
		}
	}
}