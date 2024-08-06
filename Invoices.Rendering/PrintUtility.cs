using System;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Invoices.Rendering;

public class PrintUtility
{
	public static void Print(BitmapSource image)
	{
		var printDialog = new PrintDialog();

		bool? result = printDialog.ShowDialog();

		if (result ?? false)
		{
			var imagePresenter = new Image();

			imagePresenter.Source = image;
			imagePresenter.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
			imagePresenter.VerticalAlignment = System.Windows.VerticalAlignment.Top;

			printDialog.PrintVisual(imagePresenter, "Receipt");
		}
	}
}