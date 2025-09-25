using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Invoices.Rendering.Receipt;

using Invoices.Core;
using Invoices.Rendering.Plan;
using Invoices.Rendering.Utility;

public class ReceiptInvoiceRenderer : InvoiceRenderer
{
	const int DPI = 203;
	const int WidthMM = 72;

	public override BitmapSource RenderImage(Invoice invoice)
	{
		Console.WriteLine("Creating plan");

		var plan = CreatePlan(invoice);

		Console.WriteLine("Plan has {0} elements", plan.Items.Count);

		int pixelWidth = (int)Math.Ceiling(WidthMM * DPI / 25.4);

		Console.WriteLine("Pixel width is: {0}", pixelWidth);

		int pixelHeight = plan.MeasureHeight(pixelWidth);

		Console.WriteLine("Measured pixel height is: {0}", pixelHeight);
		Console.WriteLine("Constructing Visual...");

		var visual = ConstructVisual(plan, pixelWidth);

		Console.WriteLine("Rendering Visual of size {0}x{1}", pixelWidth, pixelHeight);

		var bitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, 96, 96, PixelFormats.Pbgra32);

		visual.Measure(new Size(pixelWidth, pixelHeight));
		visual.Arrange(new Rect(0, 0, pixelWidth, pixelHeight));

		bitmap.Render(visual);

		return bitmap;
	}

	public override RenderPlan CreatePlan(Invoice invoice)
	{
		var plan = new RenderPlan();

		plan.Items.Add(new RenderPlanItem(RenderPlanItemType.Image, Assets.GetPath("Logo elements receipt.png")));

		string invoiceNumber = "Invoice #" + invoice.InvoiceNumber;
		string invoiceDate = invoice.InvoiceDateUTC.ToString("yyyy-MM-dd");
		int spaces = StandardFont.LineCharacterWidth - invoiceNumber.Length - invoiceDate.Length;

		plan.Items.Add(new RenderPlanItem(RenderPlanItemType.Text, invoiceNumber + new string(' ', spaces) + invoiceDate));
		plan.Items.Add(new RenderPlanItem(RenderPlanItemType.Text, ""));

		if ((invoice.DueDateUTC is DateTime dueDateUTC)
		 && (dueDateUTC != DateTime.MinValue))
		{
			plan.Items.Add(new RenderPlanItem(RenderPlanItemType.Text, "Due: " + dueDateUTC.ToLocalTime().ToString("yyyy-MM-dd")));
			plan.Items.Add(new RenderPlanItem(RenderPlanItemType.Text, ""));
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

		plan.Items.Add(new RenderPlanItem(RenderPlanItemType.BoldText,
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

			plan.Items.Add(new RenderPlanItem(RenderPlanItemType.Text,
				descriptionFirstLine.PadRight(ColumnWidth_Description) +
				qtyText.PadLeft(ColumnWidth_Qty) +
				priceText.PadLeft(ColumnWidth_Price) +
				subtotalText.PadLeft(ColumnWidth_Subtotal)));

			foreach (var descriptionNextLine in descriptionLines.Skip(1))
				plan.Items.Add(new RenderPlanItem(RenderPlanItemType.Text, descriptionNextLine));
		}

		plan.Items.Add(new RenderPlanItem(RenderPlanItemType.Text, ""));

		string summaryIndent = new string(' ', 19);

		string subtotalSumText = subtotalSum.ToString("$#,##0.00");

		var total = subtotalSum;

		int summaryColumnsWidth = StandardFont.LineCharacterWidth - summaryIndent.Length;

		plan.Items.Add(new RenderPlanItem(RenderPlanItemType.Text, summaryIndent + "Subtotal".PadRight(summaryColumnsWidth - subtotalSumText.Length) + subtotalSumText));

		if (invoice.Taxes.Any())
		{
			plan.Items.Add(new RenderPlanItem(RenderPlanItemType.Text, ""));

			foreach (var tax in invoice.Taxes)
			{
				decimal taxAmount = Math.Round(subtotalSum * tax.TaxRate, 2);
				string taxAmountText = taxAmount.ToString("$#,##0.00");

				plan.Items.Add(new RenderPlanItem(RenderPlanItemType.Text, summaryIndent + $"Tax ({tax.TaxName})".PadRight(summaryColumnsWidth - taxAmountText.Length) + taxAmountText));

				total += taxAmount;
			}
		}

		string totalText = total.ToString("$#,##0.00");

		plan.Items.Add(new RenderPlanItem(RenderPlanItemType.Text, ""));
		plan.Items.Add(new RenderPlanItem(RenderPlanItemType.Text, summaryIndent + "Total".PadRight(summaryColumnsWidth - totalText.Length) + totalText));

		if (invoice.Payments.Any())
		{
			plan.Items.Add(new RenderPlanItem(RenderPlanItemType.Text, ""));

			decimal remaining = total;

			int characters = StandardFont.LineCharacterWidth - summaryIndent.Length;

			foreach (var payment in invoice.Payments)
			{
				string header = "Paid: " + payment.GetShortTypeDescription();
				string amountText = payment.Amount.ToString("$#,##0.00;($#,##0.00)");

				spaces = characters - header.Length - amountText.Length;

				plan.Items.Add(new RenderPlanItem(RenderPlanItemType.Text, summaryIndent + header + new string(' ', spaces) + amountText));

				if (payment.ReceivedDateTimeUTC.HasValue)
				{
					string formatString;

					if (payment.ReceivedDateTimeUTC.Value.TimeOfDay != TimeSpan.Zero)
						formatString = "yyyy-MM-dd HH:mm";
					else
						formatString = "yyyy-MM-dd";

					string receivedDateTimeText = payment.ReceivedDateTimeUTC.Value.ToLocalTime().ToString(formatString);

					plan.Items.Add(new RenderPlanItem(RenderPlanItemType.Text, summaryIndent + receivedDateTimeText));
				}

				remaining -= payment.Amount;
			}

			if (remaining != 0)
			{
				plan.Items.Add(new RenderPlanItem(RenderPlanItemType.Text, ""));

				string header = (remaining > 0) ? "Remaining:" : "Balance:";
				string amountText = remaining.ToString("$#,##0.00;($#,##0.00)");

				spaces = characters - header.Length - amountText.Length;

				plan.Items.Add(new RenderPlanItem(RenderPlanItemType.Text, summaryIndent + header + new string(' ', spaces) + amountText));
			}
		}

		if (invoice.Notes.Any())
		{
			plan.Items.Add(new RenderPlanItem(RenderPlanItemType.Text, ""));

			foreach (string note in invoice.Notes)
			{
				if (string.IsNullOrWhiteSpace(note))
				{
					plan.Items.Add(new RenderPlanItem(RenderPlanItemType.Text, ""));
					continue;
				}

				int indentWidth = 0;

				while ((note.Length > indentWidth) && char.IsWhiteSpace(note, indentWidth))
					indentWidth++;

				string indent = note.Substring(0, indentWidth);
				string noteText = note.Substring(indentWidth);

				foreach (string noteLine in StringUtility.WordWrap(noteText, 45 - indent.Length))
					plan.Items.Add(new RenderPlanItem(RenderPlanItemType.Text, indent + noteLine));
			}
		}

		plan.Items.Add(new RenderPlanItem(RenderPlanItemType.Text, ""));
		plan.Items.Add(new RenderPlanItem(RenderPlanItemType.Text, ""));
		plan.Items.Add(new RenderPlanItem(RenderPlanItemType.Image, Assets.GetPath("White line.png")));

		return plan;
	}

	public override UIElement ConstructVisual(RenderPlan plan, int pixelWidth)
	{
		var panel = new StackPanel();

		panel.Background = Brushes.White;
		panel.Width = pixelWidth;
		panel.HorizontalAlignment = HorizontalAlignment.Left;
		panel.VerticalAlignment = VerticalAlignment.Top;

		double y = 0;

		foreach (var item in plan.Items)
		{
			double height = item.MeasureHeight(pixelWidth);

			switch (item.ItemType)
			{
				case RenderPlanItemType.Image:
				{
					var imageElement = new Image();

					imageElement.Width = pixelWidth;
					imageElement.Height = height;
					imageElement.Source = item.LoadedImage ?? ImageLoader.LoadImage(item.Value);
					imageElement.Stretch = Stretch.Uniform;

					panel.Children.Add(imageElement);

					break;
				}
				case RenderPlanItemType.Text:
				case RenderPlanItemType.BoldText:
				{
					foreach (var line in item.FlowText(pixelWidth))
					{
						var textElement = new TextBlock();

						textElement.Width = pixelWidth;
						textElement.Height = StandardFont.LineSpacingPixels;
						textElement.FontFamily = StandardFont.Font;
						textElement.Text = line;
						textElement.FontSize = StandardFont.FontSize;

						if (item.ItemType == RenderPlanItemType.BoldText)
							textElement.FontWeight = FontWeights.Bold;

						panel.Children.Add(textElement);
					}

					break;
				}
			}

			y += height;
		}

		panel.Height = y;

		return panel!;
	}
}