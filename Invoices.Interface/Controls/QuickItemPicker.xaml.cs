using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Invoices.Interface.Controls;

public partial class QuickItemPicker : UserControl
{
	public QuickItemPicker()
	{
		InitializeComponent();

		DataContext = this;
	}

	public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(nameof(Items), typeof(IList<QuickItem>), typeof(QuickItemPicker));

	public IList<QuickItem>? Items
	{
		set
		{
			if ((value != null) && !(value is BindingList<QuickItem>))
			{
				Console.WriteLine("Wrapping in BindingList, contains {0} items", value.Count);
				value = new BindingList<QuickItem>(value);
			}

			SetValue(ItemsProperty, value);
		}
		get => GetValue(ItemsProperty) as IList<QuickItem>;
	}

	public event EventHandler<QuickItem>? QuickItemActivated;

	void cmdTemplate_Click(object? sender, RoutedEventArgs e)
	{
		if ((sender is FrameworkElement senderElement)
		 && (senderElement.DataContext is QuickItem quickItem))
			QuickItemActivated?.Invoke(this, quickItem);
	}
}
