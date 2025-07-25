using System;
using System.Threading.Tasks;

using Invoices.Integration.Square;

class Program
{
	static async Task Main()
	{
		var config = new SquareConfiguration();

		config.AccessToken = "EAAAl7IZavtlBJY0WG7vIxk-1aix8_kYmFuERz8CiXQapHfUZx0QasiDuslPtdbN";
		config.EndPoint = "https://connect.squareup.com/";

		var client = new SquareRecentPaymentDataSource(config);

		await foreach (var p in client.GetRecentPayments(TimeSpan.FromSeconds(9000)))
		{
			Console.WriteLine(p);
		}
	}
}