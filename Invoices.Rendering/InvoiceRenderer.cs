using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Invoices.Core;

namespace Invoices.Rendering;

using Invoices.Rendering.Plan;
using Invoices.Rendering.Text;
using Invoices.Rendering.Utility;

public abstract class InvoiceRenderer
{
	public abstract double DisplayMargin { get; }

	protected abstract int PagePixelWidth { get; }
	protected abstract int PagePixelHeight { get; }
	protected abstract int MarginPixels { get; }

	public abstract RenderPlan CreatePlan(Invoice invoice);

	public BitmapSource RenderImage(Invoice invoice)
	{
		Console.WriteLine("Creating plan");

		var plan = CreatePlan(invoice);

		Console.WriteLine("Plan has {0} header elements, {1} body elements", plan.Header.Items.Count, plan.Body.Items.Count);

		int pixelWidth = PagePixelWidth;

		Console.WriteLine("Pixel width is: {0}", pixelWidth);

		int pixelHeight = plan.MeasureHeight(pixelWidth) + 2 * MarginPixels;

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

	public IEnumerable<BitmapSource> RenderPages(Invoice invoice)
	{
		Console.WriteLine("Creating plan");

		var plan = CreatePlan(invoice);

		Console.WriteLine("Plan has {0} header elements, {1} body elements", plan.Header.Items.Count, plan.Body.Items.Count);

		int pixelWidth = PagePixelWidth;
		int pixelHeight = PagePixelHeight;

		Console.WriteLine("Pixel width is: {0}", pixelWidth);
		Console.WriteLine("Pixel height is: {0}", pixelHeight);

		int pageNumber = 1;
		int itemIndex = 0;

		while (itemIndex < invoice.Items.Count)
		{
			Console.WriteLine("[Page " + pageNumber + "] Constructing Visual...");

			int pageStartItemIndex = itemIndex;

			var visual = ConstructVisual(plan, ref itemIndex, pageNumber, pixelHeight);

			if (itemIndex == pageStartItemIndex)
			{
				// Couldn't fit even one item on the page?
				itemIndex++;
				continue;
			}

			Console.WriteLine("Rendering Visual of size {0}x{1}", pixelWidth, pixelHeight);

			var bitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, 96, 96, PixelFormats.Pbgra32);

			visual.Measure(new Size(pixelWidth, pixelHeight));
			visual.Arrange(new Rect(0, 0, pixelWidth, pixelHeight));

			bitmap.Render(visual);

			yield return bitmap;

			pageNumber++;
		}
	}

	public UIElement ConstructVisual(RenderPlan plan)
	{
		int itemIndex = 0;

		return ConstructVisual(plan, ref itemIndex, pageNumber: 1, pagePixelHeight: int.MaxValue);
	}

	public UIElement ConstructVisual(RenderPlan plan, ref int itemIndex, int pageNumber, int pagePixelHeight)
	{
		int contentPixelWidth = PagePixelWidth - 2 * MarginPixels;

		var panel = new StackPanel();

		panel.Background = Brushes.White;
		panel.Width = contentPixelWidth;
		panel.HorizontalAlignment = HorizontalAlignment.Left;
		panel.VerticalAlignment = VerticalAlignment.Top;
		panel.Margin = new Thickness(MarginPixels);

		double y = 0;

		foreach (var item in plan.Header.Items)
		{
			panel.Children.Add(ConstructVisual(item, contentPixelWidth, out var height));

			y += height;
		}

		while (itemIndex < plan.Body.Items.Count)
		{
			var item = plan.Body.Items[itemIndex];

			var visual = ConstructVisual(item, contentPixelWidth, out var height);

			if (y + height > pagePixelHeight)
				break;

			panel.Children.Add(visual);

			y += height;
			itemIndex++;
		}

		if (pagePixelHeight == int.MaxValue)
			panel.Height = y;
		else
			panel.Height = pagePixelHeight;

		return panel!;
	}

	FrameworkElement ConstructVisual(RenderPlanItem item, int pixelWidth, out double height)
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

						TextBlock textElement = new TextBlock();

						textElement.Height = typeface.LineSpacingPixels;
						textElement.FontFamily = font.Font;
						textElement.FontSize = typeface.FontSize;
						textElement.FontWeight = typeface.Typeface.Weight;

						foreach (var line in TextUtility.FlowText(pixelWidth, value.Value, typeface))
						{
							if (textElement.Inlines.Count > 0)
								textElement.Inlines.Add(new LineBreak());

							textElement.Inlines.Add(new Run(line));
						}

						return textElement;
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

					var subItemVisual = ConstructVisual(subItem, subPixelWidth, out var subItemHeight);

					grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(subPixelWidth) });
					grid.Children.Add(subItemVisual);

					switch (item.GridParameters?.GetColumnAlignment(columnIndex))
					{
						case AlignmentX.Center: subItemVisual.HorizontalAlignment = HorizontalAlignment.Center; break;
						case AlignmentX.Right: subItemVisual.HorizontalAlignment = HorizontalAlignment.Right; break;
					}

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
		return new FrameworkElement();
	}
}