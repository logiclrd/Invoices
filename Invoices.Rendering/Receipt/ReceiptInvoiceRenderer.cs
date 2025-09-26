using System;
using System.Linq;

namespace Invoices.Rendering.Receipt;

using Invoices.Core;
using Invoices.Rendering.Plan;
using Invoices.Rendering.Utility;

public class ReceiptInvoiceRenderer : InvoiceRenderer
{
	const int DPI = 203;
	const int WidthMM = 72;

	protected override int PixelWidth => (int)Math.Ceiling(WidthMM * DPI / 25.4);
	protected override int MarginPixels => 0;

	public override RenderPlan CreatePlan(Invoice invoice)
	{
		var plan = new RenderPlan(StandardFont.SingletonInstance);

		// TODO: fix up

		plan.AddItem(RenderPlanValue.Image(Assets.GetPath("Logo elements receipt.png")));

		string invoiceNumber = "Invoice #" + invoice.InvoiceNumber;
		string invoiceDate = invoice.InvoiceDateUTC.ToString("yyyy-MM-dd");
		int spaces = plan.DefaultFont.LineCharacterWidth - invoiceNumber.Length - invoiceDate.Length;

		plan.AddItem(RenderPlanValue.Text(invoiceNumber + new string(' ', spaces) + invoiceDate));
		plan.AddItem(RenderPlanValue.Text(""));

		if ((invoice.DueDateUTC is DateTime dueDateUTC)
		 && (dueDateUTC != DateTime.MinValue))
		{
			plan.AddItem(RenderPlanValue.Text("Due: " + dueDateUTC.ToLocalTime().ToString("yyyy-MM-dd")));
			plan.AddItem(RenderPlanValue.Text(""));
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

		plan.AddItem(RenderPlanValue.BoldText(
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

			plan.AddItem(RenderPlanValue.Text(
				descriptionFirstLine.PadRight(ColumnWidth_Description) +
				qtyText.PadLeft(ColumnWidth_Qty) +
				priceText.PadLeft(ColumnWidth_Price) +
				subtotalText.PadLeft(ColumnWidth_Subtotal)));

			foreach (var descriptionNextLine in descriptionLines.Skip(1))
				plan.AddItem(RenderPlanValue.Text(descriptionNextLine));
		}

		plan.AddItem(RenderPlanValue.Text(""));

		string summaryIndent = new string(' ', 19);

		string subtotalSumText = subtotalSum.ToString("$#,##0.00");

		var total = subtotalSum;

		int summaryColumnsWidth = plan.DefaultFont.LineCharacterWidth - summaryIndent.Length;

		plan.AddItem(RenderPlanValue.Text(summaryIndent + "Subtotal".PadRight(summaryColumnsWidth - subtotalSumText.Length) + subtotalSumText));

		if (invoice.Taxes.Any())
		{
			plan.AddItem(RenderPlanValue.Text(""));

			foreach (var tax in invoice.Taxes)
			{
				decimal taxAmount = Math.Round(subtotalSum * tax.TaxRate, 2);
				string taxAmountText = taxAmount.ToString("$#,##0.00");

				plan.AddItem(RenderPlanValue.Text(summaryIndent + $"Tax ({tax.TaxName})".PadRight(summaryColumnsWidth - taxAmountText.Length) + taxAmountText));

				total += taxAmount;
			}
		}

		string totalText = total.ToString("$#,##0.00");

		plan.AddItem(RenderPlanValue.Text(""));
		plan.AddItem(RenderPlanValue.Text(summaryIndent + "Total".PadRight(summaryColumnsWidth - totalText.Length) + totalText));

		if (invoice.Payments.Any())
		{
			plan.AddItem(RenderPlanValue.Text(""));

			decimal remaining = total;

			int characters = plan.DefaultFont.LineCharacterWidth - summaryIndent.Length;

			foreach (var payment in invoice.Payments)
			{
				string header = "Paid: " + payment.GetShortTypeDescription();
				string amountText = payment.Amount.ToString("$#,##0.00;($#,##0.00)");

				spaces = characters - header.Length - amountText.Length;

				plan.AddItem(RenderPlanValue.Text(summaryIndent + header + new string(' ', spaces) + amountText));

				if (payment.ReceivedDateTimeUTC.HasValue)
				{
					string formatString;

					if (payment.ReceivedDateTimeUTC.Value.TimeOfDay != TimeSpan.Zero)
						formatString = "yyyy-MM-dd HH:mm";
					else
						formatString = "yyyy-MM-dd";

					string receivedDateTimeText = payment.ReceivedDateTimeUTC.Value.ToLocalTime().ToString(formatString);

					plan.AddItem(RenderPlanValue.Text(summaryIndent + receivedDateTimeText));
				}

				remaining -= payment.Amount;
			}

			if (remaining != 0)
			{
				plan.AddItem(RenderPlanValue.Text(""));

				string header = (remaining > 0) ? "Remaining:" : "Balance:";
				string amountText = remaining.ToString("$#,##0.00;($#,##0.00)");

				spaces = characters - header.Length - amountText.Length;

				plan.AddItem(RenderPlanValue.Text(summaryIndent + header + new string(' ', spaces) + amountText));
			}
		}

		if (invoice.Notes.Any())
		{
			plan.AddItem(RenderPlanValue.Text(""));

			foreach (string note in invoice.Notes)
			{
				if (string.IsNullOrWhiteSpace(note))
				{
					plan.AddItem(RenderPlanValue.Text(""));
					continue;
				}

				int indentWidth = 0;

				while ((note.Length > indentWidth) && char.IsWhiteSpace(note, indentWidth))
					indentWidth++;

				string indent = note.Substring(0, indentWidth);
				string noteText = note.Substring(indentWidth);

				foreach (string noteLine in StringUtility.WordWrap(noteText, 45 - indent.Length))
					plan.AddItem(RenderPlanValue.Text(indent + noteLine));
			}
		}

		plan.AddItem(RenderPlanValue.Text(""));
		plan.AddItem(RenderPlanValue.Text(""));
		plan.AddItem(RenderPlanValue.Image(Assets.GetPath("White line.png")));

		return plan;
	}
}