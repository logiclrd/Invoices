using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Invoices.Interface.Controls;

using Invoices.Core;

public partial class InvoiceList : UserControl
{
	public InvoiceList()
	{
		InitializeComponent();

		DataContext = this;
	}

	public static DependencyProperty InvoicesProperty = DependencyProperty.Register(nameof(Invoices), typeof(IEnumerable<Invoice>), typeof(InvoiceList));

	public IEnumerable<Invoice> Invoices
	{
		get => (IEnumerable<Invoice>)GetValue(InvoicesProperty);
		set => SetValue(InvoicesProperty, value);
	}
}