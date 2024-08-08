using System.Windows;

namespace Invoices.Interface;

using Invoices.Core;

public partial class MainWindow : Window
{
	Database _database;

	public MainWindow()
	{
		InitializeComponent();

		_database = new Database();

		ilInvoices.Invoices = _database.LoadInvoices();
	}
}