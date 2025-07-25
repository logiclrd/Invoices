using System;
using System.Collections.Generic;

namespace Invoices.Integration;

public interface IPaymentDataSource
{
	public string Name { get; }
	public IAsyncEnumerable<ExternalPayment> GetRecentPayments(TimeSpan window);
}
