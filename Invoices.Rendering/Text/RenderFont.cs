using System.Windows;
using System.Windows.Media;

namespace Invoices.Rendering.Text;

public abstract class RenderFont
{
	public readonly string FontFamilyName;

	public RenderFont(string fontFamilyName)
	{
		FontFamilyName = fontFamilyName;
	}

	FontFamily? s_font;

	public FontFamily Font => s_font ??= new FontFamily(FontFamilyName);

	public double LineSpacingPixels => Font.LineSpacing * FontSize;
	public abstract int LineCharacterWidth { get; }
	public abstract double FontSize { get; }

	RenderTypeface? s_typeface;
	RenderTypeface? s_typefaceBold;
	RenderTypeface? s_typefaceTitle;

	public RenderTypeface Typeface => s_typeface ??= new RenderTypeface(this, new Typeface(Font, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal));
	public RenderTypeface TypefaceBold => s_typefaceBold ??= new RenderTypeface(this, new Typeface(Font, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal));
	public RenderTypeface TypefaceTitle => s_typefaceTitle ??= new RenderTypeface(this, new Typeface(Font, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal), sizeFactor: 1.6);

	public RenderTypeface GetTypeface(TypefaceType typefaceType)
	{
		switch (typefaceType)
		{
			case TypefaceType.Regular: return Typeface;
			case TypefaceType.Bold: return TypefaceBold;
			case TypefaceType.Title: return TypefaceTitle;

			default: goto case TypefaceType.Regular;
		}
	}
}
