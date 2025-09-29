using System;
using System.Linq;

using Invoices.Core;

namespace Invoices.Rendering.Receipt;

using Invoices.Rendering.Plan;
using Invoices.Rendering.Utility;

public class ReceiptInvoiceRenderer : InvoiceRenderer
{
	public override string Title => "Receipt";

	public override string DefaultPrintQueueName => "POS-80C";

	const int WidthMM = 72;

	public override int DPI => 203;

	public override double DisplayMargin => 30;

	public override bool IsContinuous => true;

	protected override int PagePixelWidth => (int)Math.Ceiling(WidthMM * DPI / 25.4);
	protected override int PagePixelHeight => 0;
	protected override int MarginPixels => 0;

	public override RenderPlan CreatePlan(Invoice invoice)
	{
		var plan = new RenderPlan(StandardFont.SingletonInstance);

		var body = plan.Body;

		body.AddItem(RenderPlanValue.Image(Assets.GetPath("Logo elements receipt.png")));

		string invoiceNumber = "Invoice #" + invoice.InvoiceNumber;
		string invoiceDate = invoice.InvoiceDateUTC.ToString("yyyy-MM-dd");
		int spaces = plan.DefaultFont.LineCharacterWidth - invoiceNumber.Length - invoiceDate.Length;

		body.AddItem(RenderPlanValue.Text(invoiceNumber + new string(' ', spaces) + invoiceDate));
		body.AddItem(RenderPlanValue.Text(""));

		if ((invoice.DueDateUTC is DateTime dueDateUTC)
		 && (dueDateUTC != DateTime.MinValue))
		{
			body.AddItem(RenderPlanValue.Text("Due: " + dueDateUTC.ToLocalTime().ToString("yyyy-MM-dd")));
			body.AddItem(RenderPlanValue.Text(""));
		}

		int ColumnWidth_Description = 21;
		int ColumnWidth_Qty = 6;
		int ColumnWidth_Price = 9;
		int ColumnWidth_Subtotal = 9;

		if (invoice.Items.Any(item => item.UnitPrice >= 1000))
		{
			ColumnWidth_Description--;
			ColumnWidth_Price++;
		}

		if (invoice.Items.Sum(item => item.UnitPrice * item.Quantity) >= 1000)
		{
			ColumnWidth_Description--;
			ColumnWidth_Subtotal++;
		}

		int maxQuantityLength = invoice.Items.Max(item => item.Quantity.ToString("#,##0.##").Length);

		if (maxQuantityLength + 2 > ColumnWidth_Qty)
		{
			int delta = maxQuantityLength + 2 - ColumnWidth_Qty;

			ColumnWidth_Description -= delta;
			ColumnWidth_Qty += delta;
		}

		body.AddItem(RenderPlanValue.BoldText(
			"Description".PadRight(ColumnWidth_Description) +
			"Qty".PadRight(ColumnWidth_Qty) +
			"Price".PadRight(ColumnWidth_Price) +
			"Subtotal".PadLeft(ColumnWidth_Subtotal)));

		decimal subtotalSum = 0;

		foreach (var item in invoice.Items)
		{
			var descriptionLines = StringUtility.WordWrap(item.Description ?? "", ColumnWidth_Description - 1);

			var subtotal = item.UnitPrice * item.Quantity;

			var descriptionFirstLine = descriptionLines.First();
			var qtyText = item.Quantity.ToString("#,##0.##") + " @";
			var priceText = item.UnitPrice.ToString("$#,##0.00");
			var subtotalText = subtotal.ToString("$#,##0.00");

			subtotalSum += subtotal;

			body.AddItem(RenderPlanValue.Text(
				descriptionFirstLine.PadRight(ColumnWidth_Description) +
				qtyText.PadLeft(ColumnWidth_Qty) +
				priceText.PadLeft(ColumnWidth_Price) +
				subtotalText.PadLeft(ColumnWidth_Subtotal)));

			foreach (var descriptionNextLine in descriptionLines.Skip(1))
				body.AddItem(RenderPlanValue.Text(descriptionNextLine));
		}

		body.AddItem(RenderPlanValue.Text(""));

		string summaryIndent = new string(' ', 19);

		string subtotalSumText = subtotalSum.ToString("$#,##0.00");

		var total = subtotalSum;

		int summaryColumnsWidth = plan.DefaultFont.LineCharacterWidth - summaryIndent.Length;

		body.AddItem(RenderPlanValue.Text(summaryIndent + "Subtotal".PadRight(summaryColumnsWidth - subtotalSumText.Length) + subtotalSumText));

		if (invoice.Taxes.Any())
		{
			body.AddItem(RenderPlanValue.Text(""));

			foreach (var tax in invoice.Taxes)
			{
				decimal taxAmount = Math.Round(subtotalSum * tax.TaxRate, 2);
				string taxAmountText = taxAmount.ToString("$#,##0.00");

				body.AddItem(RenderPlanValue.Text(summaryIndent + $"Tax ({tax.TaxName})".PadRight(summaryColumnsWidth - taxAmountText.Length) + taxAmountText));

				total += taxAmount;
			}
		}

		string totalText = total.ToString("$#,##0.00");

		body.AddItem(RenderPlanValue.Text(""));
		body.AddItem(RenderPlanValue.Text(summaryIndent + "Total".PadRight(summaryColumnsWidth - totalText.Length) + totalText));

		if (invoice.Payments.Any())
		{
			body.AddItem(RenderPlanValue.Text(""));

			decimal remaining = total;

			int characters = plan.DefaultFont.LineCharacterWidth - summaryIndent.Length;

			foreach (var payment in invoice.Payments)
			{
				string header = "Paid: " + payment.GetShortTypeDescription();
				string amountText = payment.Amount.ToString("$#,##0.00;($#,##0.00)");

				spaces = characters - header.Length - amountText.Length;

				if (spaces > 0)
					body.AddItem(RenderPlanValue.Text(summaryIndent + header + new string(' ', spaces) + amountText));
				else
				{
					foreach (string line in StringUtility.WordWrap(header, summaryColumnsWidth))
						body.AddItem(RenderPlanValue.Text(summaryIndent + line));
					body.AddItem(RenderPlanValue.Text(amountText.PadLeft(plan.DefaultFont.LineCharacterWidth)));
				}

				if (payment.ReceivedDateTimeUTC.HasValue)
				{
					string formatString;

					if (payment.ReceivedDateTimeUTC.Value.TimeOfDay != TimeSpan.Zero)
						formatString = "yyyy-MM-dd HH:mm";
					else
						formatString = "yyyy-MM-dd";

					string receivedDateTimeText = payment.ReceivedDateTimeUTC.Value.ToLocalTime().ToString(formatString);

					body.AddItem(RenderPlanValue.Text(summaryIndent + receivedDateTimeText));
				}

				remaining -= payment.Amount;
			}

			if (remaining != 0)
			{
				body.AddItem(RenderPlanValue.Text(""));

				string header = (remaining > 0) ? "Remaining:" : "Balance:";
				string amountText = remaining.ToString("$#,##0.00;($#,##0.00)");

				spaces = characters - header.Length - amountText.Length;

				body.AddItem(RenderPlanValue.Text(summaryIndent + header + new string(' ', spaces) + amountText));
			}
		}

		if (invoice.Notes.Any())
		{
			body.AddItem(RenderPlanValue.Text(""));

			foreach (string note in invoice.Notes)
			{
				if (string.IsNullOrWhiteSpace(note))
				{
					body.AddItem(RenderPlanValue.Text(""));
					continue;
				}

				int indentWidth = 0;

				while ((note.Length > indentWidth) && char.IsWhiteSpace(note, indentWidth))
					indentWidth++;

				string indent = note.Substring(0, indentWidth);
				string noteText = note.Substring(indentWidth);

				foreach (string noteLine in StringUtility.WordWrap(noteText, 45 - indent.Length))
					body.AddItem(RenderPlanValue.Text(indent + noteLine));
			}
		}

		body.AddItem(RenderPlanValue.Text(""));
		body.AddItem(RenderPlanValue.Text(""));
		body.AddItem(RenderPlanValue.Image(Assets.GetPath("White line.png")));

		return plan;
	}
}