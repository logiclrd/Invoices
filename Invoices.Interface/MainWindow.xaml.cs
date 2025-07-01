using System;
using System.ComponentModel;
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

	protected override void OnClosing(CancelEventArgs e)
	{
		int modifiedItems = 0;
		int firstModifiedItemIndex = -1;
		InvoiceEditor? firstModifiedEditor = null;

		for (int i = 0; i < tcRoot.Items.Count; i++)
		{
			if (tcRoot.Items[i] is TabItem tiTab)
			{
				if (tiTab.Header is InvoiceTabHeader ithHeader)
				{
					if (ithHeader.IsModified)
					{
						modifiedItems++;

						if (firstModifiedItemIndex < 0)
						{
							firstModifiedItemIndex = i;
							firstModifiedEditor = tiTab.Content as InvoiceEditor;
						}
					}
				}
			}
		}

		if (modifiedItems > 0)
		{
			if ((modifiedItems == 1) && (firstModifiedEditor != null))
			{
				tcRoot.SelectedIndex = firstModifiedItemIndex;

				if (!firstModifiedEditor.PromptSaveInvoice())
					e.Cancel = true;
			}
			else
			{
				var result = MessageBox.Show("There are " + modifiedItems + " modified invoices. Do you wish to save them all?", "Modified", MessageBoxButton.YesNoCancel);

				if (result == MessageBoxResult.No)
				{
					result = MessageBox.Show("Really quit and discard all changes?", "Modified", MessageBoxButton.YesNo);

					if (result == MessageBoxResult.No)
						e.Cancel = true;
				}
				else if (result == MessageBoxResult.Cancel)
					e.Cancel = true;
				else
				{
					// Save everything
					try
					{
						for (int i = 0; i < tcRoot.Items.Count; i++)
						{
							if (tcRoot.Items[i] is TabItem tiTab)
							{
								if (tiTab.Content is InvoiceEditor ieInvoice)
								{
									if (ieInvoice.IsModified)
									{
										tcRoot.SelectedIndex = i;
										ieInvoice.SaveInvoice();
									}
								}
							}
						}
					}
					catch (Exception ex)
					{
						MessageBox.Show("An error occurred: " + ex, "Error During Save", MessageBoxButton.OK);

						e.Cancel = true;
					}
				}
			}
		}
	}
}