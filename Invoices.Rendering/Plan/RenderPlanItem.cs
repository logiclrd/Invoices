using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Invoices.Rendering.Plan;

using Invoices.Rendering.Receipt;

public class RenderPlanItem
{
	public RenderPlanItemType ItemType;
	public string Value;

	public RenderPlanItem(RenderPlanItemType itemType, string value)
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

		for (int i = 0; i < Value.Length; i++)
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
						(ItemType == RenderPlanItemType.BoldText) ? StandardFont.TypefaceBold : StandardFont.Typeface,
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
			case RenderPlanItemType.Image:
				if (LoadedImage == null)
					LoadedImage = ImageLoader.LoadImage(Value);

				return LoadedImage.PixelHeight * pixelWidth / LoadedImage.PixelWidth;
			case RenderPlanItemType.Text:
			case RenderPlanItemType.BoldText:
				return FlowText(pixelWidth).Count() * StandardFont.LineSpacingPixels;
			default:
				return 0;
		}
	}
}

