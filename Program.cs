using System;
using System.IO;
using System.Xml.Serialization;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Invoices;

class Program
{
	[STAThread]
	static void Main(string[] args)
	{
		if (args.Length == 0)
		{
			Console.WriteLine("Usage: Invoices <invoicefile.xml>");
			return;
		}

		string invoiceFilePath = args[0];

		if (!File.Exists(invoiceFilePath))
		{
			Console.WriteLine("Couldn't find file: {0}", invoiceFilePath);
			return;
		}

		//new Application().Run(new MainWindow());
		var serializer = new XmlSerializer(typeof(Invoice));

		Invoice invoice;

		using (var stream = File.OpenRead(invoiceFilePath))
			invoice = (Invoice)serializer.Deserialize(stream)!;

		var renderer = new InvoiceRenderer();

		var renderedInvoice = renderer.RenderImage(invoice);

		var encoder = new PngBitmapEncoder();

		encoder.Frames.Add(BitmapFrame.Create(renderedInvoice));

		using (var outputStream = File.OpenWrite("Invoice #" + invoice.InvoiceNumber + ".png"))
			encoder.Save(outputStream);

		Print(renderedInvoice);
	}

	static void Print(BitmapSource image)
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
