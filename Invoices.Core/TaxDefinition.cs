namespace Invoices.Core;

public class TaxDefinition
{
	public int TaxID { get; set; }
	public string? TaxName { get; set; }
	public decimal TaxRate { get; set; }
}