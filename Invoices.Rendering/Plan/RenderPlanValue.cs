using System;
using System.Linq;
using System.Windows.Media.Imaging;

namespace Invoices.Rendering.Plan;

using Invoices.Rendering.Text;
using Invoices.Rendering.Utility;

public class RenderPlanValue
{
	public string? Value;
	public RenderPlanValueType Type;

	public static implicit operator RenderPlanValue(string v) => new RenderPlanValue(v);

	public RenderPlanValue(string value)
		: this(RenderPlanValueType.Text, value)
	{
	}

	public RenderPlanValue(RenderPlanValueType type, string value)
	{
		Type = type;
		Value = value;
	}

	public BitmapSource? LoadedImage;

	public static RenderPlanValue Text(string text) => new RenderPlanValue(RenderPlanValueType.Text, text);
	public static RenderPlanValue BoldText(string text) => new RenderPlanValue(RenderPlanValueType.BoldText, text);
	public static RenderPlanValue TitleText(string text) => new RenderPlanValue(RenderPlanValueType.TitleText, text);
	public static RenderPlanValue Image(string resourceName) => new RenderPlanValue(RenderPlanValueType.Image, resourceName);

	public double MeasureHeight(int pixelWidth, RenderFont font)
	{
		if (pixelWidth == 0)
			return 0;

		switch (Type)
		{
			case RenderPlanValueType.Image:
				if (LoadedImage == null)
					LoadedImage = ImageLoader.LoadImage(Value);

				return LoadedImage.PixelHeight * pixelWidth / LoadedImage.PixelWidth;
			case RenderPlanValueType.Text:
			case RenderPlanValueType.BoldText:
			case RenderPlanValueType.TitleText:
				var typefaceType =
					Type switch
					{
						RenderPlanValueType.Text => TypefaceType.Regular,
						RenderPlanValueType.BoldText => TypefaceType.Bold,
						RenderPlanValueType.TitleText => TypefaceType.Title,

						_ => throw new Exception("Sanity failure")
					};

				return TextUtility.FlowText(pixelWidth, Value, font.GetTypeface(typefaceType)).Count() * font.LineSpacingPixels;

			default: return 0;
		}
	}
}
