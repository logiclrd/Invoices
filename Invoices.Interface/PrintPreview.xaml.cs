using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Invoices.Interface;

using Invoices.Core;
using Invoices.Rendering;

public partial class PrintPreview : Window
{
	public PrintPreview()
	{
		InitializeComponent();
	}

	BitmapSource? _renderedInvoice;

	public void LoadInvoice(Invoice invoice)
	{
		var renderer = new InvoiceRenderer();

		_renderedInvoice = renderer.RenderImage(invoice);

		imgPreview.Source = _renderedInvoice;
	}

	bool _dragging = false;
	Point _dragStart;
	Point _scrollAtDragStart;

	void imgPreview_MouseDown(object? sender, MouseButtonEventArgs e)
	{
		if (e.ChangedButton == MouseButton.Left)
		{
			_dragging = true;
			_dragStart = Mouse.GetPosition(this);
			_scrollAtDragStart = new Point(svPresentation.HorizontalOffset, svPresentation.VerticalOffset);
			Mouse.Capture(imgPreview);
		}
	}

	void imgPreview_MouseMove(object? sender, MouseEventArgs e)
	{
		if (_dragging)
		{
			var newPosition = Mouse.GetPosition(this);

			svPresentation.ScrollToHorizontalOffset(_scrollAtDragStart.X - newPosition.X + _dragStart.X);
			svPresentation.ScrollToVerticalOffset(_scrollAtDragStart.Y - newPosition.Y + _dragStart.Y);
		}
	}

	void imgPreview_MouseUp(object? sender, MouseButtonEventArgs e)
	{
		if (_dragging)
		{
			Mouse.Capture(null);
			_dragging = false;
		}
	}

	void cmdPrint_Click(object? sender, RoutedEventArgs e)
	{
		if (_renderedInvoice != null)
			PrintUtility.Print(_renderedInvoice);
	}

	void cmdClose_Click(object? sender, RoutedEventArgs e)
	{
		DialogResult = false;
	}
}