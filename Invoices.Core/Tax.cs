namespace Invoices;

public class Tax
{
	public int? TaxID;
	public string? TaxName;
	public decimal TaxRate;

	public static Tax Rehydrate(TaxDefinition taxDefinition)
		=> Rehydrate(taxDefinition.TaxID, taxDefinition.TaxName, taxDefinition.TaxRate);

	public static Tax Rehydrate(int? taxID, string? taxName, decimal taxRate)
	{
		return
			new Tax()
			{
				TaxID = taxID,
				TaxName = taxName,
				TaxRate = taxRate,
			};
	}
}