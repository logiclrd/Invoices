using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using Invoices.Core;

using Invoices.Rendering;

namespace Invoices.Interface;

using Invoices.Interface.Utility;

public partial class PrintPreview : Window
{
	InvoiceRenderer _renderer;

	public PrintPreview(InvoiceRenderer renderer)
	{
		InitializeComponent();

		_renderer = renderer;

		imgPreview.Margin = new Thickness(renderer.DisplayMargin);
	}

	BitmapSource? _renderedInvoice;

	public void LoadInvoice(Invoice invoice)
	{
		_renderedInvoice = _renderer.RenderImage(invoice);

		imgPreview.Source = _renderedInvoice;
	}

	void UpdateScale()
	{
		if (_renderedInvoice == null)
			return;

		double actualWidth = imgPreview.Margin.Left + _renderedInvoice.PixelWidth + imgPreview.Margin.Right;
		double viewportWidth = svPresentation.ViewportWidth;

		if (viewportWidth <= 0)
			return;

		if (actualWidth <= viewportWidth)
			ccScale.LayoutTransform = null;
		else
		{
			double scale = viewportWidth / actualWidth;

			ccScale.LayoutTransform = new ScaleTransform(scale, scale);
		}
	}

	protected override void OnContentRendered(EventArgs e)
	{
		UpdateScale();
	}

	protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
	{
		Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, UpdateScale);
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
			PrintUtility.Print(_renderer, _renderedInvoice);
	}

	void cmdClose_Click(object? sender, RoutedEventArgs e)
	{
		DialogResult = false;
	}
}