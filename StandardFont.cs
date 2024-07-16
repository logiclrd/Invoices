using System.Windows;
using System.Windows.Media;

namespace Invoices;

public class StandardFont
{
	public const string FontFamilyName = "Roboto Mono";

	public static FontFamily LoadFont()
	{
		return new FontFamily(FontFamilyName);
	}

	public static FontFamily Font = new FontFamily(FontFamilyName);

	public static double LineSpacingPixels => Font.LineSpacing * FontSize;
	public static int LineCharacterWidth = 45;

	public static Typeface Typeface = new Typeface(Font, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
	public static Typeface TypefaceBold = new Typeface(Font, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

	public static double FontSize = 21.25;
}