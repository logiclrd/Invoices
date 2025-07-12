using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Invoices.Interface.Controls;

using Invoices.Core;

public partial class ItemTemplatePicker : UserControl
{
	public ItemTemplatePicker()
	{
		InitializeComponent();

		DataContext = this;
	}

	public static readonly DependencyProperty ItemTemplatesProperty = DependencyProperty.Register(nameof(ItemTemplates), typeof(IList<ItemTemplate>), typeof(ItemTemplatePicker));

	public IList<ItemTemplate>? ItemTemplates
	{
		set
		{
			if ((value != null) && !(value is BindingList<ItemTemplate>))
			{
				Console.WriteLine("Wrapping in BindingList, contains {0} items", value.Count);
				value = new BindingList<ItemTemplate>(value);
			}

			SetValue(ItemTemplatesProperty, value);
		}
		get => GetValue(ItemTemplatesProperty) as IList<ItemTemplate>;
	}

	public event EventHandler<ItemTemplate>? ItemTemplateActivated;

	void cmdTemplate_Click(object? sender, RoutedEventArgs e)
	{
		if ((sender is FrameworkElement senderElement)
		 && (senderElement.DataContext is ItemTemplate template))
			ItemTemplateActivated?.Invoke(this, template);
	}
}
