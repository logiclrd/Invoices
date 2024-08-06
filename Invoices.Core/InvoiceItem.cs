namespace Invoices.Core;

public class InvoiceItem
{
	public string? Description;
	public int Quantity;
	public decimal UnitPrice;

	public static InvoiceItem Rehydrate((int InvoiceID, int Sequence, string Description, int Quantity, decimal UnitPrice) data)
		=> Rehydrate(data.Description, data.Quantity, data.UnitPrice);

	public static InvoiceItem Rehydrate(string description, int quantity, decimal unitPrice)
	{
		return
			new InvoiceItem()
			{
				Description = description,
				Quantity = quantity,
				UnitPrice = unitPrice,
			};
	}
}