using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Invoices.Rendering;

public class RenderedDocument
{
	public string DocumentName;
	public FrameworkElement[] Pages;

	public RenderedDocument(string documentName, params FrameworkElement[] pages)
	{
		DocumentName = documentName;
		Pages = pages;
	}

	public static RenderedDocument FromImage(string documentName, BitmapSource image)
	{
		var imagePresenter = new Image();

		imagePresenter.Source = image;
		imagePresenter.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
		imagePresenter.VerticalAlignment = System.Windows.VerticalAlignment.Top;

		return new RenderedDocument(documentName, imagePresenter);
	}
}

