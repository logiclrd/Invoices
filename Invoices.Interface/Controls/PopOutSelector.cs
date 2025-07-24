using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace Invoices.Interface.Controls;

public class PopOutSelector : ContentControl
{
	ToggleButton tbToggle;
	StackPanel spButtonContent;
	TextBlock tbLabel;
	Popup pPicker;
	Grid gSelector;

	public PopOutSelector()
	{
		tbToggle = new ToggleButton();
		tbToggle.Checked += tbToggle_Checked;
		tbToggle.Unchecked += tbToggle_Unchecked;
		tbToggle.SizeChanged += tbToggle_SizeChanged;
		tbToggle.SetBinding(ToggleButton.IsCheckedProperty, new Binding() { Source = this, Path = new PropertyPath(IsTogglePressedProperty) });

		spButtonContent = new StackPanel();

		tbLabel = new TextBlock();
		tbLabel.SetBinding(TextBlock.TextProperty, new Binding() { Source = this, Path = new PropertyPath(ButtonLabelProperty) });

		pPicker = new Popup();

		gSelector = new Grid();
		gSelector.Background = new SolidColorBrush(Color.FromArgb(192, 255, 255, 255));

		pPicker.Child = gSelector;
		pPicker.AllowsTransparency = true;
		pPicker.StaysOpen = false;
		pPicker.Placement = PlacementMode.Bottom;
		pPicker.PlacementTarget = tbToggle;
		pPicker.SetBinding(Popup.IsOpenProperty, new Binding() { Source = this, Path = new PropertyPath(IsTogglePressedProperty) });

		spButtonContent.Children.Add(tbLabel);
		spButtonContent.Children.Add(pPicker);

		tbToggle.Content = spButtonContent;

		this.Content = tbToggle;

		AddHandler(Button.ClickEvent, new RoutedEventHandler(AnyButtonClicked));
	}

	void AnyButtonClicked(object? sender, RoutedEventArgs e)
	{
		if ((e.OriginalSource != tbToggle) && CloseOnAnyButtonClick)
			IsTogglePressed = false;
	}

	public static DependencyProperty IsTogglePressedProperty = DependencyProperty.Register(nameof(IsTogglePressed), typeof(bool), typeof(PopOutSelector));
	public static DependencyProperty ButtonLabelProperty = DependencyProperty.Register(nameof(ButtonLabel), typeof(string), typeof(PopOutSelector), new PropertyMetadata() { DefaultValue = "" });
	public static DependencyProperty SelectorProperty = DependencyProperty.Register(nameof(Selector), typeof(UIElement), typeof(PopOutSelector), new PropertyMetadata() { PropertyChangedCallback = SelectorChanged });
	public static DependencyProperty CloseOnAnyButtonClickProperty = DependencyProperty.Register(nameof(CloseOnAnyButtonClick), typeof(bool), typeof(PopOutSelector), new PropertyMetadata() { DefaultValue = true });

	static void SelectorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var @this = (PopOutSelector)d;

		@this.gSelector.Children.Clear();
		@this.gSelector.Children.Add((UIElement)e.NewValue);
	}

	public bool IsTogglePressed
	{
		get => (bool)GetValue(IsTogglePressedProperty);
		set => SetValue(IsTogglePressedProperty, value);
	}

	public string ButtonLabel
	{
		get => (string)GetValue(ButtonLabelProperty);
		set => SetValue(ButtonLabelProperty, value);
	}

	public UIElement? Selector
	{
		get => (UIElement?)GetValue(SelectorProperty);
		set => SetValue(SelectorProperty, value);
	}

	public bool CloseOnAnyButtonClick
	{
		get => (bool)GetValue(CloseOnAnyButtonClickProperty);
		set => SetValue(CloseOnAnyButtonClickProperty, value);
	}

	void tbToggle_Checked(object? sender, RoutedEventArgs e)
	{
		IsTogglePressed = true;
	}

	void tbToggle_Unchecked(object? sender, RoutedEventArgs e)
	{
		IsTogglePressed = false;
	}

	void tbToggle_SizeChanged(object? sender, SizeChangedEventArgs e)
	{
		pPicker.Width = e.NewSize.Width;
	}
}