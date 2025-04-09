using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Invoices.Interface;

using Invoices.Core;
using Invoices.Interface.Controls;

public partial class CustomerPicker : Window
{
	public CustomerPicker()
	{
		InitializeComponent();

		DataContext = this;

		ceEditor.Customer = new Customer();
	}

	public static readonly DependencyProperty CustomersProperty = DependencyProperty.Register(nameof(Customers), typeof(IList<Customer>), typeof(CustomerPicker));

	public IList<Customer>? Customers
	{
		get => (IList<Customer>?)GetValue(CustomersProperty);
		set
		{
			SetValue(CustomersProperty, value);

			var customersView = (CollectionView)CollectionViewSource.GetDefaultView(value);

			customersView.Filter = TypeFilter;
		}
	}

	string? _filterText;

	bool TypeFilter(object item)
	{
		if (string.IsNullOrWhiteSpace(_filterText))
			return true;

		if (!(item is Customer customer))
			return true;

		if (customer.Name.Any(n => n.Contains(_filterText, StringComparison.CurrentCultureIgnoreCase)))
			return true;
		if (customer.Address.Any(n => n.Contains(_filterText, StringComparison.CurrentCultureIgnoreCase)))
			return true;
		if (customer.PhoneNumbers.Any(n => n.Contains(_filterText, StringComparison.CurrentCultureIgnoreCase)))
			return true;
		if (customer.EmailAddresses.Any(n => n.Contains(_filterText, StringComparison.CurrentCultureIgnoreCase)))
			return true;
		if (customer.Notes.Any(n => n.Contains(_filterText, StringComparison.CurrentCultureIgnoreCase)))
			return true;

		return false;
	}

	void txtCustomerFilter_TextChanged(object? sender, RoutedEventArgs e)
	{
		_filterText = txtCustomerFilter.Text;
		CollectionViewSource.GetDefaultView(lvCustomers.ItemsSource).Refresh();
	}

	void Window_PreviewTextInput(object? sender, TextCompositionEventArgs e)
	{
		if (!(Keyboard.FocusedElement is TextBox))
			txtCustomerFilter.Focus();
	}

	Customer? _selectedCustomer;

	public Customer? SelectedCustomer
	{
		get => _selectedCustomer;
		set
		{
			_selectedCustomer = value;

			if (_selectedCustomer == null)
			{
				Pick();
				lvCustomers.SelectedIndex = -1;
			}
			else
			{
				int selectedCustomerID = _selectedCustomer.CustomerID;

				if (this.Customers is IList<Customer> customers)
				{
					for (int i=0; i < customers.Count; i++)
						if (customers[i].CustomerID == selectedCustomerID)
						{
							lvCustomers.SelectedIndex = i;
							break;
						}
				}

				if (selectedCustomerID > 0)
					Edit();
				else
					Pick();
			}
		}
	}

	public event EventHandler? CreateOrUpdateSelectedCustomer;

	void lvCustomers_SelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		_selectedCustomer = lvCustomers.SelectedItem as Customer;

		cmdEdit.IsEnabled = (_selectedCustomer != null);
	}

	void lvCustomers_MouseDoubleClick(object? sender, MouseButtonEventArgs e)
	{
		if (e.ChangedButton == MouseButton.Left)
			Accept();
	}

	void ceEditor_PreviewKeyDown(object? sender, KeyEventArgs e)
	{
		if ((e.Key == Key.Enter) && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
		{
			e.Handled = true;

			// This ensures that anything the user has just typed gets pushed back to the DataContext.
			cmdOK.Focus();

			Accept();
		}
	}

	bool _editing = false;

	void BeginEdit()
	{
		dpPickCustomer.Visibility = Visibility.Collapsed;
		dpAddEditCustomer.Visibility = Visibility.Visible;

		cmdAdd.Visibility = Visibility.Collapsed;
		cmdEdit.Visibility = (_selectedCustomer != null) ? Visibility.Visible : Visibility.Collapsed;
		cmdPick.Visibility = Visibility.Visible;

		_editing = true;
	}

	void Add()
	{
		BeginEdit();

		ceEditor.Customer = new Customer();
	}

	void Edit()
	{
		BeginEdit();

		ceEditor.Customer = _selectedCustomer;
	}

	void Pick()
	{
		dpAddEditCustomer.Visibility = Visibility.Collapsed;
		dpPickCustomer.Visibility = Visibility.Visible;

		cmdAdd.Visibility = Visibility.Visible;
		cmdEdit.Visibility = (_selectedCustomer != null) ? Visibility.Visible : Visibility.Collapsed;
		cmdPick.Visibility = Visibility.Collapsed;

		_editing = false;
	}

	void cmdAdd_Click(object? sender, RoutedEventArgs e)
	{
		Add();
	}

	void cmdEdit_Click(object? sender, RoutedEventArgs e)
	{
		Edit();
	}

	void cmdPick_Click(object? sender, RoutedEventArgs e)
	{
		Pick();
	}

	void cmdOK_Click(object? sender, RoutedEventArgs e) => Accept();
	void cmdCancel_Click(object? sender, RoutedEventArgs e) => Cancel();

	void Accept()
	{
		if (_editing)
		{
			_selectedCustomer = ceEditor.Customer;

			CreateOrUpdateSelectedCustomer?.Invoke(this, EventArgs.Empty);
		}

		DialogResult = true;
	}

	void Cancel()
	{
		DialogResult = false;
	}
}
