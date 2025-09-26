using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Invoices.Rendering.Utility;

public class TextUtility
{
	public static IEnumerable<string> FlowText(int pixelWidth, string? text, RenderTypeface typeface)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			yield return "";
			yield break;
		}

		var line = new StringBuilder();

		string lastAcceptedString = "";

		for (int i = 0; i < text.Length; i++)
		{
			if (!char.IsWhiteSpace(text, i))
				line.Append(text[i]);
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
						typeface.Typeface,
						typeface.Font.FontSize,
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
}
