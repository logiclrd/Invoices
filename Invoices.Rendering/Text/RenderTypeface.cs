using System.Windows.Media;

namespace Invoices.Rendering;

using Invoices.Rendering.Text;

public class RenderTypeface
{
	public readonly RenderFont Font;
	public readonly Typeface Typeface;
	public readonly double SizeFactor;

	public double FontSize => Font.FontSize * SizeFactor;
	public double LineSpacingPixels => Font.LineSpacingPixels * SizeFactor;

	public RenderTypeface(RenderFont font, Typeface typeface, double sizeFactor = 1.0)
	{
		Font = font;
		Typeface = typeface;
		SizeFactor = sizeFactor;
	}
}