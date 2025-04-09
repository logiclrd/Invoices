using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

	public event EventHandler? Save;
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

	void grdRoot_MouseDown(object? sender, MouseButtonEventArgs e)
	{
		if (e.ChangedButton == MouseButton.Middle)
		{
			Save?.Invoke(this, EventArgs.Empty);
			Close?.Invoke(this, EventArgs.Empty);
		}
	}

	void cmdClose_Click(object? sender, RoutedEventArgs e)
	{
		if (IsModified)
		{
			var result = MessageBox.Show("Save changes?", "Modified", MessageBoxButton.YesNoCancel);

			if (result == MessageBoxResult.Yes)
				Save?.Invoke(this, EventArgs.Empty);
			if (result == MessageBoxResult.Cancel)
				return;
		}

		Close?.Invoke(this, EventArgs.Empty);
	}
}
