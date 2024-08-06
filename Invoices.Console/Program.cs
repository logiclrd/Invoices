using System;
using System.IO;
using System.Xml.Serialization;

namespace Invoices.Console;

using Konsole = System.Console;

using Invoices.Core;
using Invoices.Rendering;

class Program
{
	[STAThread]
	static void Main(string[] args)
	{
		if (args.Length == 0)
		{
			Konsole.WriteLine("Usage: Invoices <invoicefile.xml>");
			return;
		}

		var database = new Database();

		int invoiceID;

		string invoiceFilePath = args[0];

		if (!File.Exists(invoiceFilePath))
		{
			if (invoiceFilePath.StartsWith("db:"))
			{
				string invoiceNumber = invoiceFilePath.Substring(3);

				invoiceID = database.GetInvoiceIDByInvoiceNumber(invoiceNumber);

				Konsole.WriteLine("Invoice #{0} has ID {1}", invoiceNumber, invoiceID);
			}
			else
			{
				Konsole.WriteLine("Couldn't find file: {0}", invoiceFilePath);
				return;
			}
		}
		else
		{
			//new Application().Run(new MainWindow());
			var serializer = new XmlSerializer(typeof(Invoice));

			Invoice invoiceFromFile;

			using (var stream = File.OpenRead(invoiceFilePath))
				invoiceFromFile = (Invoice)serializer.Deserialize(stream)!;

			database.SaveInvoice(invoiceFromFile);

			invoiceID = invoiceFromFile.InvoiceID;
		}

		var invoice = database.LoadInvoice(invoiceID);

		var renderer = new InvoiceRenderer();

		var renderedInvoice = renderer.RenderImage(invoice);

		ImageUtility.SavePng(renderedInvoice, "Invoice #" + invoice.InvoiceNumber + ".png");
		PrintUtility.Print(renderedInvoice);
	}
}
