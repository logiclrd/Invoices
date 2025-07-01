namespace Invoices.Core;

public class ItemTemplate
{
	public int ItemTemplateID { get; set; }
	public string? Category { get; set; }
	public string? Description { get; set; }
	public decimal UnitPrice { get; set; }
}
