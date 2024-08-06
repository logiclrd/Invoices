using System;
using System.Windows;

namespace Invoices.Interface;

class Program
{
	[STAThread]
	static void Main()
	{
		new Application().Run(new MainWindow());
	}
}
