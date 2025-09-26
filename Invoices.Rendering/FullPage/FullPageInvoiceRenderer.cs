using System.Windows;

namespace Invoices.Rendering.FullPage;

using System;
using Invoices.Core;
using Invoices.Rendering.Plan;

public class FullPageInvoiceRenderer : InvoiceRenderer
{
	public const int DPI = 300;

	public const double PageWidth = 8.5;
	public const double PageHeight = 11;

	public const double Margin = 0.75;

  protected override int PixelWidth => (int)(PageWidth * DPI);
  protected override int MarginPixels => (int)(Margin * DPI);

	public readonly int PageWidthPixels = (int)Math.Ceiling((PageWidth - 2 * Margin) * DPI);
	public readonly int PageHeightPixels = (int)Math.Ceiling((PageHeight - 2 * Margin) * DPI);

	public override RenderPlan CreatePlan(Invoice invoice)
	{
		var plan = new RenderPlan(StandardFont.SingletonInstance);

		var headingParameters = RenderGridParameters.Create(PageWidthPixels, -1, PageWidthPixels * 3 / 5);

		var heading = RenderPlanItem.GridRow(headingParameters);

		heading.AddItem(RenderPlanItem.Stack(
			RenderPlanItem.TitleText("Invoice #" + invoice.InvoiceNumber),
			RenderPlanItem.Text(invoice.InvoiceDateUTC.ToLocalTime().ToString("yyyy-MM-dd"))));

		heading.AddItem(RenderPlanItem.Image(Assets.GetPath("Logo elements coloured.png")));

		return plan;
	}
}