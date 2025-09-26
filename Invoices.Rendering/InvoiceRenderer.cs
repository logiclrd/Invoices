using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Invoices.Core;

namespace Invoices.Rendering;

using Invoices.Rendering.Plan;
using Invoices.Rendering.Text;
using Invoices.Rendering.Utility;

public abstract class InvoiceRenderer
{
	protected abstract int PixelWidth { get; }
	protected abstract int MarginPixels { get; }

	public abstract RenderPlan CreatePlan(Invoice invoice);

	public BitmapSource RenderImage(Invoice invoice)
	{
		Console.WriteLine("Creating plan");

		var plan = CreatePlan(invoice);

		Console.WriteLine("Plan has {0} elements", plan.Items.Count);

		int pixelWidth = PixelWidth;

		Console.WriteLine("Pixel width is: {0}", pixelWidth);

		int pixelHeight = plan.MeasureHeight(pixelWidth);

		Console.WriteLine("Measured pixel height is: {0}", pixelHeight);
		Console.WriteLine("Constructing Visual...");

		var visual = ConstructVisual(plan);

		Console.WriteLine("Rendering Visual of size {0}x{1}", pixelWidth, pixelHeight);

		var bitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, 96, 96, PixelFormats.Pbgra32);

		visual.Measure(new Size(pixelWidth, pixelHeight));
		visual.Arrange(new Rect(0, 0, pixelWidth, pixelHeight));

		bitmap.Render(visual);

		return bitmap;
	}

	public UIElement ConstructVisual(RenderPlan plan)
	{
		int pixelWidth = PixelWidth;

		var panel = new StackPanel();

		panel.Background = Brushes.White;
		panel.Width = pixelWidth;
		panel.HorizontalAlignment = HorizontalAlignment.Left;
		panel.VerticalAlignment = VerticalAlignment.Top;
		panel.Margin = new Thickness(MarginPixels);

		double y = 0;

		foreach (var item in plan.Items)
		{
			panel.Children.Add(ConstructVisual(item, pixelWidth, out var height));

			y += height;
		}

		panel.Height = y;

		return panel!;
	}

	UIElement ConstructVisual(RenderPlanItem item, int pixelWidth, out double height)
	{
		switch (item.ItemType)
		{
			case RenderPlanItemType.Value:
			{
				height = item.MeasureHeight(pixelWidth);

				var value = item.Value;

				if (value == null)
					break;

				switch (value.Type)
				{
					case RenderPlanValueType.Image:
					{
						var imageElement = new Image();

						imageElement.Width = pixelWidth;
						imageElement.Height = height;
						imageElement.Source = value.LoadedImage ?? ImageLoader.LoadImage(value.Value);
						imageElement.Stretch = Stretch.Uniform;

						return imageElement;
					}
					case RenderPlanValueType.Text:
					case RenderPlanValueType.BoldText:
					case RenderPlanValueType.TitleText:
					{
						var typefaceType =
							value.Type switch
							{
								RenderPlanValueType.Text => TypefaceType.Regular,
								RenderPlanValueType.BoldText => TypefaceType.Bold,
								RenderPlanValueType.TitleText => TypefaceType.Title,

								_ => throw new Exception("Sanity failure")
							};

						var font = item.Font;
						var typeface = font.GetTypeface(typefaceType);

						var stack = new StackPanel();

						foreach (var line in TextUtility.FlowText(pixelWidth, value.Value, typeface))
						{
							var textElement = new TextBlock();

							textElement.Width = pixelWidth;
							textElement.Height = font.LineSpacingPixels;
							textElement.FontFamily = font.Font;
							textElement.Text = line;
							textElement.FontSize = typeface.FontSize;
							textElement.FontWeight = typeface.Typeface.Weight;

							stack.Children.Add(textElement);
						}

						return stack;
					}
				}

				break;
			}
			case RenderPlanItemType.Stack:
			{
				var stack = new StackPanel();

				height = 0;

				foreach (var subItem in item.Items)
				{
					stack.Children.Add(ConstructVisual(subItem, pixelWidth, out var subItemHeight));

					height += subItemHeight;
				}

				return stack;
			}
			case RenderPlanItemType.GridRow:
			{
				var grid = new Grid();

				height = 0;

				for (int columnIndex = 0; columnIndex < item.Items.Count; columnIndex++)
				{
					var subItem = item.Items[columnIndex];

					int subPixelWidth = item.GridParameters?.GetColumnPixelWidth(columnIndex, pixelWidth) ?? (pixelWidth / item.Items.Count);

					var subItemVisual = ConstructVisual(subItem, pixelWidth, out var subItemHeight);

					grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(subPixelWidth) });
					grid.Children.Add(subItemVisual);

					Grid.SetColumn(subItemVisual, columnIndex);

					height = Math.Max(height, subItemHeight);
				}

				grid.Width = pixelWidth;
				grid.Height = height;

				return grid;
			}
		}

		// ??
		height = 0;
		return new UIElement();
	}
}