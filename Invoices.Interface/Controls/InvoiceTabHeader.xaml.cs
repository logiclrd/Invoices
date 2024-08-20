using System;
using System.Windows;
using System.Windows.Controls;

namespace Invoices.Interface.Controls;

public partial class InvoiceTabHeader : UserControl
{
	public InvoiceTabHeader()
	{
		InitializeComponent();

		DataContext = this;
	}

	public static DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(InvoiceTabHeader));
	public static DependencyProperty IsModifiedProperty = DependencyProperty.Register(nameof(IsModified), typeof(bool), typeof(InvoiceTabHeader));

	public event EventHandler? Close;

	public string? Title
	{
		get => (string?)GetValue(TitleProperty);
		set => SetValue(TitleProperty, value);
	}

	public bool IsModified
	{
		get => (bool)GetValue(IsModifiedProperty);
		set => SetValue(IsModifiedProperty, value);
	}

	void cmdClose_Click(object? sender, RoutedEventArgs e)
	{
		Close?.Invoke(this, EventArgs.Empty);
	}
}
