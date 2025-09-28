using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using Invoices.Core;

namespace Invoices.Rendering.FullPage;

using Invoices.Rendering.Plan;

public class FullPageInvoiceRenderer : InvoiceRenderer
{
	public override string Title => "Invoice";

	public override string DefaultPrintQueueName => "Microsoft Print to PDF";

	public override int DPI => 96;

	public const double PageWidth = 8.5;
	public const double PageHeight = 11;

	public const double Margin = 0.75;

	public override double DisplayMargin => 0;

	public override Size PageSizeInches => new Size(PageWidth, PageHeight);

	public override bool IsContinuous => false;

	protected override int PagePixelWidth => (int)(PageWidth * DPI);
	protected override int PagePixelHeight => (int)(PageHeight * DPI);
	protected override int MarginPixels => (int)(Margin * DPI);

	public int ContentPixelWidth => (int)Math.Ceiling((PageWidth - 2 * Margin) * DPI);
	public int ContentPixelHeight => (int)Math.Ceiling((PageHeight - 2 * Margin) * DPI);

	public const double LogoImageHeightInches = 2.2;

	public override RenderPlan CreatePlan(Invoice invoice)
	{
		var plan = new RenderPlan(StandardFont.SingletonInstance);

		var headingParameters = RenderGridParameters.Create(ContentPixelWidth, -1, ContentPixelWidth * 2 / 5);

		headingParameters.Columns[1].Alignment = AlignmentX.Right;

		var topLeftStack = RenderPlanItem.Stack(
			RenderPlanItem.BoldText("Wizards of the Plains"),
			RenderPlanItem.Text("120-15 Cooper's Town Road"),
			RenderPlanItem.Text("Winnipeg, MB"),
			RenderPlanItem.Text("R3Y 2E3"),
			RenderPlanItem.BoldText("info@wizardsoftheplains.ca"),
			RenderPlanItem.Text("(431) 887-4096"),
			RenderPlanItem.Text("https://www.wizardsoftheplains.ca/"));

		var logoImage = RenderPlanItem.Image(Assets.GetPath("Logo elements coloured.png"), maxHeight: LogoImageHeightInches * DPI);

		var invoiceInfoStack = RenderPlanItem.Stack(
			RenderPlanItem.TitleText(""),
			RenderPlanItem.TitleText("Invoice #" + invoice.InvoiceNumber),
			RenderPlanItem.Text(invoice.InvoiceDateUTC.ToLocalTime().ToString("yyyy-MM-dd")));

		if (invoice.InvoiceeCustomer is Customer customer)
		{
			invoiceInfoStack.AddItem(RenderPlanItem.Text(""));
			foreach (var line in customer.LongSummaryLines)
				invoiceInfoStack.AddItem(RenderPlanItem.Text(line));
		}

		plan.Header.AddGridRow(
			headingParameters,
			topLeftStack,
			logoImage);

		plan.Header.AddItem(RenderPlanItem.Text(""));

		plan.Body.AddItem(invoiceInfoStack);

		// In inches
		const double QtyWidth = 1;
		const double UnitPriceWidth = 1;
		const double TotalWidth = 1;
		const double LabelWidth = 1.2;

		const double SummaryLineWidth = LabelWidth + TotalWidth;

		var itemGridHeadingParameters = RenderGridParameters.Create(ContentPixelWidth, -1, QtyWidth * DPI, UnitPriceWidth * DPI, TotalWidth * DPI);
		var itemGridParameters = RenderGridParameters.Create(ContentPixelWidth, -1, QtyWidth * DPI, UnitPriceWidth * DPI, TotalWidth * DPI);
		var itemGridSummaryContainerParameters = RenderGridParameters.Create(ContentPixelWidth, -1, SummaryLineWidth * DPI);
		var itemGridSummaryParameters = RenderGridParameters.Create((int)(SummaryLineWidth * DPI), LabelWidth * DPI, TotalWidth * DPI);

		itemGridHeadingParameters.Columns[1].Alignment = AlignmentX.Center;
		itemGridHeadingParameters.Columns[2].Alignment = AlignmentX.Right;
		itemGridHeadingParameters.Columns[3].Alignment = AlignmentX.Right;

		itemGridParameters.Columns[1].Alignment = AlignmentX.Center;
		itemGridParameters.Columns[2].Alignment = AlignmentX.Right;
		itemGridParameters.Columns[3].Alignment = AlignmentX.Right;

		itemGridSummaryParameters.Columns[1].Alignment = AlignmentX.Right;

		plan.Body.AddItem(RenderPlanItem.Text(""));

		plan.Body.AddGridRow(
			itemGridHeadingParameters,
			RenderPlanItem.BoldText("Description"),
			RenderPlanItem.BoldText("Qty"),
			RenderPlanItem.BoldText("Price"),
			RenderPlanItem.BoldText("Total"));

		decimal subtotal = 0.0M;

		foreach (var item in invoice.Items)
		{
			plan.Body.AddGridRow(
				itemGridParameters,
				RenderPlanItem.Text(item.Description ?? "Unknown Item"),
				RenderPlanItem.Text(item.Quantity.ToString()),
				RenderPlanItem.Text(item.UnitPrice.ToString("$#,##0.00")),
				RenderPlanItem.Text(item.LineTotal.ToString("$#,##0.00")));

			subtotal += item.UnitPrice * item.Quantity;
		}

		var summaryStack = RenderPlanItem.Stack();

		summaryStack.AddItem(RenderPlanItem.Text(""));

		summaryStack.AddGridRow(
			itemGridSummaryParameters,
			RenderPlanItem.BoldText("Subtotal:"),
			RenderPlanItem.Text(subtotal.ToString("$#,##0.00")));

		decimal total = subtotal;

		foreach (var tax in invoice.Taxes)
		{
			decimal taxAmount = tax.TaxRate * subtotal;

			summaryStack.AddGridRow(
				itemGridSummaryParameters,
				RenderPlanItem.Text("  " + tax.TaxName + " (" + tax.TaxRate.ToString("P") + ")"),
				RenderPlanItem.Text(taxAmount.ToString("$#,##0.00")));

			total += taxAmount;
		}

		summaryStack.AddItem(RenderPlanItem.Text(""));

		summaryStack.AddGridRow(
			itemGridSummaryParameters,
			RenderPlanItem.BoldText("Total:"),
			RenderPlanItem.Text(total.ToString("$#,##0.00")));

		if (invoice.Payments.Any())
		{
			summaryStack.AddItem(RenderPlanItem.Text(""));

			summaryStack.AddGridRow(
				itemGridSummaryParameters,
				RenderPlanItem.BoldText("Payments:"));

			foreach (var payment in invoice.Payments)
			{
				summaryStack.AddGridRow(
					itemGridSummaryParameters,
					RenderPlanItem.Text("  " + payment.GetShortTypeDescription()),
					RenderPlanItem.Text((-payment.Amount).ToString("$#,##0.00")));

				total -= payment.Amount;
			}

			summaryStack.AddItem(RenderPlanItem.Text(""));

			summaryStack.AddGridRow(
				itemGridSummaryParameters,
				RenderPlanItem.BoldText((total >= 0) ? "Outstanding:" : "Balance:"),
				RenderPlanItem.Text(total.ToString("$#,##0.00")));
		}

		var notesStack = RenderPlanItem.Stack();

		if (invoice.Notes.Where(note => !string.IsNullOrWhiteSpace(note)).Any())
		{
			notesStack.AddItem(RenderPlanItem.Text(""));
			notesStack.AddItem(RenderPlanItem.BoldText("Notes"));

			foreach (var noteLine in invoice.Notes)
				notesStack.AddItem(RenderPlanItem.Text(noteLine));
		}

		plan.Body.AddGridRow(
			itemGridSummaryContainerParameters,
			notesStack,
			summaryStack);

		var footerParameters = RenderGridParameters.Create(ContentPixelHeight, -1);

		footerParameters.Columns[0].Alignment = AlignmentX.Center;

		plan.Body.AddItem(RenderPlanItem.Text(""));

		plan.Body.AddGridRow(
			footerParameters,
			RenderPlanItem.Text("Thank you for your business!"));

		return plan;
	}
}