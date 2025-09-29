using System;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Invoices.Core;
using Invoices.Rendering;

namespace Invoices.Interface.Utility;

public class PrintUtility
{
	public static void Print(Window owner, InvoiceRenderer renderer, Invoice invoice)
	{
		if (renderer.IsContinuous)
		{
			var dialog = new PrintPreview(renderer);

			dialog.Owner = owner;
			dialog.LoadInvoice(invoice);

			dialog.ShowDialog();
		}
		else
		{
			var printServer = new PrintServer();

			var printQueue = printServer.GetPrintQueues().FirstOrDefault(queue => queue.Name == renderer.DefaultPrintQueueName);

			var pages = renderer.RenderPages(invoice).ToArray();

			Print(owner, printQueue, renderer.Title, renderer.PageSizeInches, renderer.DPI, pages);
		}
	}

	public static void Print(InvoiceRenderer renderer, BitmapSource image)
	{
		var printServer = new PrintServer();

		var printQueue = printServer.GetPrintQueues().FirstOrDefault(queue => queue.Name == renderer.DefaultPrintQueueName);

		Print(printQueue, renderer.Title, image);
	}

	public static void Print(PrintQueue? printQueue, string title, BitmapSource image)
	{
		var printDialog = new PrintDialog();

		if (printQueue != null)
			printDialog.PrintQueue = printQueue;

		bool? result = printDialog.ShowDialog();

		if (result ?? false)
		{
			var imagePresenter = new Image();

			imagePresenter.Source = image;
			imagePresenter.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
			imagePresenter.VerticalAlignment = System.Windows.VerticalAlignment.Top;

			printDialog.PrintVisual(imagePresenter, title);
		}
	}

	public static void Print(Window owner, PrintQueue? printQueue, string title, Size pageSizeInches, int dpi, BitmapSource image)
	{
		var imagePresenter = new Image();

		imagePresenter.Source = image;
		imagePresenter.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
		imagePresenter.VerticalAlignment = System.Windows.VerticalAlignment.Top;

		Print(owner, printQueue, title, pageSizeInches, dpi, imagePresenter);
	}

	public static void Print(Window owner, PrintQueue? printQueue, string title, Size pageSizeInches, int dpi, params FrameworkElement[] pages)
	{
		var printDialogX = new PrintDialogX.PrintDialog.PrintDialog();

		printDialogX.Owner = owner;
		printDialogX.Title = title;

		printDialogX.DefaultPrinter = printQueue;

		printDialogX.AllowPagesOption = true; //Allow the "Pages" option (contains "All Pages", "Current Page", and "Custom Pages")
		printDialogX.AllowPagesPerSheetOption = false; //Allow the "Pages Per Sheet" option
		printDialogX.AllowPageOrderOption = false; //Allow the "Page Order" option
		printDialogX.AllowScaleOption = false; //Allow the "Scale" option
		printDialogX.AllowDoubleSidedOption = true; //Allow the "Double-Sided" option
		printDialogX.AllowAddNewPrinterButton = false; //Allow the "Add New Printer" button in the printer list
		printDialogX.AllowPrinterPreferencesButton = true; //Allow the "Printer Preferences" button

		void GenerateDocument()
		{
			var documentX = new PrintDialogX.PrintDocument();

			if ((pageSizeInches.Height == 0) && (pages.Length > 0))
			{
				pages[0].Measure(new Size(pageSizeInches.Width * dpi, double.MaxValue));
				pageSizeInches.Height = pages[0].DesiredSize.Height / dpi;
			}

			documentX.SetSizeByInch(pageSizeInches.Width, pageSizeInches.Height);
			documentX.DocumentMargin = 0; // margin is baked into the visual

			foreach (var page in pages)
			{
				var pageX = new PrintDialogX.PrintPage();

				pageX.Content = page;

				RenderOptions.SetBitmapScalingMode(page, BitmapScalingMode.HighQuality);

				documentX.Pages.Add(pageX);
			}

			printDialogX.Document = documentX;
		}

		try
		{
			printDialogX.ShowDialog(GenerateDocument);
		}
		catch { }
	}
}