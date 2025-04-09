using System;
using System.Globalization;
using System.Threading;
using System.Windows;

namespace Invoices.Interface;

class Program
{
	[STAThread]
	static void Main()
	{
		var cultureInfo = CultureInfo.CreateSpecificCulture(CultureInfo.CurrentCulture.Name);

		cultureInfo.DateTimeFormat.ShortDatePattern = "yyyy-MM-dd";

		Thread.CurrentThread.CurrentCulture = cultureInfo;

		new Application().Run(new MainWindow());
	}
}
