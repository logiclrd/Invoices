using System;
using System.Windows.Controls;

namespace Invoices.Interface.Controls;

using Invoices.Core;

public partial class CustomerEditor : UserControl
{
	public CustomerEditor()
	{
		InitializeComponent();
	}

	Customer? _customer;

	public Customer? Customer
	{
		get => _customer;
		set
		{
			_customer = value;
			DataContext = _customer;
		}
	}
}