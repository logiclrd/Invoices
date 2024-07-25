using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Invoices;

public class InvoiceRenderer
{
	const int DPI = 203;
	const int WidthMM = 72;

	class RenderPlan
	{
		public List<RenderPlanItem> Items = new List<RenderPlanItem>();

		public int MeasureHeight(int pixelWidth)
		{
			double height = 0;

			foreach (var item in Items)
				height += item.MeasureHeight(pixelWidth);

			return (int)height;
		}
	}

	enum ItemType
	{
		Image,
		Text,
		BoldText,
	}

	class RenderPlanItem
	{
		public ItemType ItemType;
		public string Value;

		public RenderPlanItem(ItemType itemType, string value)
		{
			ItemType = itemType;
			Value = value;
		}

		public BitmapSource? LoadedImage;

		public IEnumerable<string> FlowText(int pixelWidth)
		{
			if (string.IsNullOrWhiteSpace(Value))
			{
				yield return "";
				yield break;
			}

			var line = new StringBuilder();

			string lastAcceptedString = "";

			for (int i=0; i < Value.Length; i++)
			{
				if (!char.IsWhiteSpace(Value, i))
					line.Append(Value[i]);
				else
				{
					string newTestString = line.ToString().TrimEnd();

					line.Append(' ');

					if (newTestString != lastAcceptedString)
					{
						var formatted = new FormattedText(
							newTestString,
							CultureInfo.CurrentCulture,
							FlowDirection.LeftToRight,
							(ItemType == ItemType.BoldText) ? StandardFont.TypefaceBold : StandardFont.Typeface,
							StandardFont.FontSize,
							Brushes.Black,
							pixelsPerDip: 1);

						if (formatted.Width <= pixelWidth)
							lastAcceptedString = newTestString;
						else
						{
							yield return lastAcceptedString;

							line.Remove(0, lastAcceptedString.Length);
							while ((line.Length > 0) && char.IsWhiteSpace(line[0]))
								line.Remove(0, 1);

							lastAcceptedString = "";
						}
					}
				}
			}

			lastAcceptedString = line.ToString();

			if (!string.IsNullOrWhiteSpace(lastAcceptedString))
				yield return lastAcceptedString;
		}

		public double MeasureHeight(int pixelWidth)
		{
			switch (ItemType)
			{
				case ItemType.Image:
					if (LoadedImage == null)
						LoadedImage = ImageLoader.LoadImage(Value);

					return LoadedImage.PixelHeight * pixelWidth / LoadedImage.PixelWidth;
				case ItemType.Text:
				case ItemType.BoldText:
					return FlowText(pixelWidth).Count() * StandardFont.LineSpacingPixels;
				default:
					return 0;
			}
		}
	}

	public BitmapSource RenderImage(Invoice invoice)
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

	RenderPlan CreatePlan(Invoice invoice)
	{
		var plan = new RenderPlan();

		plan.Items.Add(new RenderPlanItem(ItemType.Image, @"C:\code\Invoices\Logo elements receipt.png"));

		string invoiceNumber = "Invoice #" + invoice.InvoiceNumber;
		string invoiceDate = invoice.InvoiceDate.ToString("yyyy-MM-dd");
		int spaces = StandardFont.LineCharacterWidth - invoiceNumber.Length - invoiceDate.Length;

		plan.Items.Add(new RenderPlanItem(ItemType.Text, invoiceNumber + new string(' ', spaces) + invoiceDate));
		plan.Items.Add(new RenderPlanItem(ItemType.Text, ""));

		if (invoice.DueDate is DateTime dueDate)
		{
			plan.Items.Add(new RenderPlanItem(ItemType.Text, "Due: " + dueDate.ToString("yyyy-MM-dd")));
			plan.Items.Add(new RenderPlanItem(ItemType.Text, ""));
		}

		const int ColumnWidth_Description = 21;
		const int ColumnWidth_Qty = 6;
		const int ColumnWidth_Price = 9;
		const int ColumnWidth_Subtotal = 9;

		plan.Items.Add(new RenderPlanItem(ItemType.BoldText,
			"Description".PadRight(ColumnWidth_Description) +
			"Qty".PadRight(ColumnWidth_Qty) +
			"Price".PadRight(ColumnWidth_Price) +
			"Subtotal".PadLeft(ColumnWidth_Subtotal)));

		decimal subtotalSum = 0;

		foreach (var item in invoice.Items)
		{
			var descriptionLines = WordWrap(item.Description ?? "", ColumnWidth_Description - 1);

			var subtotal = item.UnitPrice * item.Quantity;

			var descriptionFirstLine = descriptionLines.First();
			var qtyText = item.Quantity + " @";
			var priceText = item.UnitPrice.ToString("$#,##0.00");
			var subtotalText = subtotal.ToString("$#,##0.00");

			subtotalSum += subtotal;

			plan.Items.Add(new RenderPlanItem(ItemType.Text,
				descriptionFirstLine.PadRight(ColumnWidth_Description) +
				qtyText.PadLeft(ColumnWidth_Qty) +
				priceText.PadLeft(ColumnWidth_Price) +
				subtotalText.PadLeft(ColumnWidth_Subtotal)));

			foreach (var descriptionNextLine in descriptionLines.Skip(1))
				plan.Items.Add(new RenderPlanItem(ItemType.Text, descriptionNextLine));
		}

		plan.Items.Add(new RenderPlanItem(ItemType.Text, ""));

		string summaryIndent = new string(' ', 19);

		string subtotalSumText = subtotalSum.ToString("$#,##0.00");

		var total = subtotalSum;

		int summaryColumnsWidth = StandardFont.LineCharacterWidth - summaryIndent.Length;

		plan.Items.Add(new RenderPlanItem(ItemType.Text, summaryIndent + "Subtotal".PadRight(summaryColumnsWidth - subtotalSumText.Length) + subtotalSumText));

		if (invoice.Taxes.Any())
		{
			plan.Items.Add(new RenderPlanItem(ItemType.Text, ""));

			foreach (var tax in invoice.Taxes)
			{
				decimal taxAmount = Math.Round(subtotalSum * tax.TaxRate, 2);
				string taxAmountText = taxAmount.ToString("$#,##0.00");

				plan.Items.Add(new RenderPlanItem(ItemType.Text, summaryIndent + $"Tax ({tax.TaxName})".PadRight(summaryColumnsWidth - taxAmountText.Length) + taxAmountText));

				total += taxAmount;
			}
		}

		string totalText = total.ToString("$#,##0.00");

		plan.Items.Add(new RenderPlanItem(ItemType.Text, ""));
		plan.Items.Add(new RenderPlanItem(ItemType.Text, summaryIndent + "Total".PadRight(summaryColumnsWidth - totalText.Length) + totalText));

		if (invoice.Payments.Any())
		{
			plan.Items.Add(new RenderPlanItem(ItemType.Text, ""));

			decimal remaining = total;

			int characters = StandardFont.LineCharacterWidth - summaryIndent.Length;

			foreach (var payment in invoice.Payments)
			{
				string header = "Paid: " + payment.GetShortTypeDescription();
				string amountText = payment.Amount.ToString("$#,##0.00");

				spaces = characters - header.Length - amountText.Length;

				plan.Items.Add(new RenderPlanItem(ItemType.Text, summaryIndent + header + new string(' ', spaces) + amountText));

				if (payment.ReceivedDateTime.HasValue)
				{
					string receivedDateTimeText = payment.ReceivedDateTime.Value.ToString("yyyy-MM-dd HH:mm");

					plan.Items.Add(new RenderPlanItem(ItemType.Text, summaryIndent + receivedDateTimeText));
				}

				remaining -= payment.Amount;
			}

			if (remaining != 0)
			{
				plan.Items.Add(new RenderPlanItem(ItemType.Text, ""));

				string header = "Remaining:";
				string amountText = remaining.ToString("$#,##0.00");

				spaces = characters - header.Length - amountText.Length;

				plan.Items.Add(new RenderPlanItem(ItemType.Text, summaryIndent + header + new string(' ', spaces) + amountText));
			}
		}

		plan.Items.Add(new RenderPlanItem(ItemType.Text, ""));
		plan.Items.Add(new RenderPlanItem(ItemType.Text, ""));
		plan.Items.Add(new RenderPlanItem(ItemType.Image, @"C:\code\Invoices\White line.png"));

		return plan;
	}

	public static IEnumerable<string> WordWrap(string text, int characterWidth)
	{
		int index = 0;

		int lineStart = 0;
		int lineEnd = 0;

		while (index < text.Length)
		{
			int lastWordEnd = index;

			while ((lastWordEnd < text.Length) && !char.IsWhiteSpace(text, lastWordEnd))
				lastWordEnd++;

			int nextWordStart = lastWordEnd;

			while ((nextWordStart < text.Length) && char.IsWhiteSpace(text, nextWordStart))
				nextWordStart++;

			int newLineLength = lastWordEnd - lineStart;

			if (newLineLength > characterWidth)
			{
				yield return text.Substring(lineStart, lineEnd - lineStart);

				lineStart = index;

				while ((lineStart < text.Length) && char.IsWhiteSpace(text, lineStart))
					lineStart++;
			}
			else
				lineEnd = lastWordEnd;

			index = nextWordStart;
		}

		while ((lineStart < text.Length) && char.IsWhiteSpace(text, lineStart))
			lineStart++;

		if (lineStart < text.Length)
			yield return text.Substring(lineStart);
	}

	UIElement ConstructVisual(RenderPlan plan, int pixelWidth)
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
				case ItemType.Image:
				{
					var imageElement = new Image();

					imageElement.Width = pixelWidth;
					imageElement.Height = height;
					imageElement.Source = item.LoadedImage ?? ImageLoader.LoadImage(item.Value);
					imageElement.Stretch = Stretch.Uniform;

					panel.Children.Add(imageElement);

					break;
				}
				case ItemType.Text:
				case ItemType.BoldText:
				{
					foreach (var line in item.FlowText(pixelWidth))
					{
						var textElement = new TextBlock();

						textElement.Width = pixelWidth;
						textElement.Height = StandardFont.LineSpacingPixels;
						textElement.FontFamily = StandardFont.Font;
						textElement.Text = line;
						textElement.FontSize = StandardFont.FontSize;

						if (item.ItemType == ItemType.BoldText)
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