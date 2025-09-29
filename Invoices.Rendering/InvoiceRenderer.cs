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
	public abstract string Title { get; }

	public abstract string DefaultPrintQueueName { get; }

	public abstract double DisplayMargin { get; }

	protected abstract int PagePixelWidth { get; }
	protected abstract int PagePixelHeight { get; }
	protected abstract int MarginPixels { get; }

	public abstract int DPI { get; }
	public virtual Size PageSizeInches => new Size(PagePixelWidth / (double)DPI, PagePixelHeight / (double)DPI);
	public abstract bool IsContinuous { get; }

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

	public RenderedDocument RenderPages(Invoice invoice)
	{
		Console.WriteLine("Creating plan");

		var plan = CreatePlan(invoice);

		Console.WriteLine("Plan has {0} header elements, {1} body elements", plan.Header.Items.Count, plan.Body.Items.Count);

		int pixelWidth = PagePixelWidth;
		int pixelHeight = PagePixelHeight;

		if (pixelHeight == 0)
			pixelHeight = plan.MeasureHeight(pixelWidth) + 2 * MarginPixels;

		Console.WriteLine("Pixel width is: {0}", pixelWidth);
		Console.WriteLine("Pixel height is: {0}", pixelHeight);

		int pageCount = 0;
		int itemIndex = 0;

		Console.WriteLine("Counting Pages...");

		while (itemIndex < invoice.Items.Count)
		{
			int pageStartItemIndex = itemIndex;

			ConstructVisual(plan, ref itemIndex, pageCount + 1, pageCount + 1, pixelHeight, dryRun: true);

			if (itemIndex == pageStartItemIndex)
			{
				// Couldn't fit even one item on the page?
				itemIndex++;
				continue;
			}

			pageCount++;
		}

		int pageNumber = 1;

		itemIndex = 0;

		List<FrameworkElement> pages = new List<FrameworkElement>(pageCount);

		while (itemIndex < invoice.Items.Count)
		{
			Console.WriteLine("[Page " + pageNumber + "] Constructing Visual...");

			int pageStartItemIndex = itemIndex;

			var visual = ConstructVisual(plan, ref itemIndex, pageNumber, pageCount, pixelHeight) ?? throw new Exception("Internal error: ConstructVisual returned null");

			if (itemIndex == pageStartItemIndex)
			{
				// Couldn't fit even one item on the page?
				itemIndex++;
				continue;
			}

			pages.Add(visual);

			pageNumber++;
		}

		return new RenderedDocument(plan.DocumentName, pages.ToArray());
	}

	public FrameworkElement ConstructVisual(RenderPlan plan)
	{
		int itemIndex = 0;

		return ConstructVisual(plan, ref itemIndex, pageNumber: 1, pageCount: 1, pagePixelHeight: InfinitePageHeight) ?? throw new Exception("Internal error: ConstructVisual returned null");
	}

	const int InfinitePageHeight = int.MaxValue;

	public FrameworkElement? ConstructVisual(RenderPlan plan, ref int itemIndex, int pageNumber, int pageCount, int pagePixelHeight, bool dryRun = false)
	{
		int contentPixelWidth = PagePixelWidth - 2 * MarginPixels;
		int contentPixelHeight = pagePixelHeight - 2 * MarginPixels;

		Canvas? page = null;
		StackPanel? panel = null;

		if (!dryRun)
		{
			page = new Canvas();

			panel = new StackPanel();

			panel.Background = Brushes.White;
			panel.Width = contentPixelWidth;
			panel.HorizontalAlignment = HorizontalAlignment.Left;
			panel.VerticalAlignment = VerticalAlignment.Top;

			Canvas.SetLeft(panel, MarginPixels);
			Canvas.SetTop(panel, MarginPixels);

			page.Children.Add(panel);

			if (pagePixelHeight != InfinitePageHeight)
			{
				var pageNumberVisual = new TextBlock();

				pageNumberVisual.Text = pageNumber + " / " + pageCount;
				pageNumberVisual.FontFamily = plan.DefaultFont.Font;
				pageNumberVisual.FontSize = plan.DefaultFont.FontSize * 0.8;
				pageNumberVisual.Foreground = Brushes.DarkSlateGray;

				pageNumberVisual.Measure(new Size(PagePixelWidth, pagePixelHeight));

				Canvas.SetLeft(pageNumberVisual, (PagePixelWidth - pageNumberVisual.DesiredSize.Width) / 2);
				Canvas.SetTop(pageNumberVisual, pagePixelHeight - MarginPixels / 2 - pageNumberVisual.DesiredSize.Height);

				page.Children.Add(pageNumberVisual);
			}
		}

		double y = 0;

		foreach (var item in plan.Header.Items)
		{
			var visual = ConstructVisual(item, contentPixelWidth, out var height, dryRun);

			if (visual != null)
				panel!.Children.Add(visual);

			y += height;
		}

		while (itemIndex < plan.Body.Items.Count)
		{
			var item = plan.Body.Items[itemIndex];

			var visual = ConstructVisual(item, contentPixelWidth, out var height, dryRun);

			if (y + height > contentPixelHeight)
				break;

			if (visual != null)
				panel!.Children.Add(visual);

			y += height;
			itemIndex++;
		}

		if (!dryRun)
		{
			if (pagePixelHeight == InfinitePageHeight)
				panel!.Height = y;
			else
				panel!.Height = contentPixelHeight;

			page!.Width = PagePixelWidth;
			page.Height = panel.Height + 2 * MarginPixels;
		}

		return page;
	}

	FrameworkElement? ConstructVisual(RenderPlanItem item, int pixelWidth, out double height, bool dryRun)
	{
		height = 0;

		switch (item.ItemType)
		{
			case RenderPlanItemType.Value:
			{
				height = item.MeasureHeight(pixelWidth);

				var value = item.Value;

				if ((value == null) || dryRun)
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
				StackPanel? stack = null;

				if (!dryRun)
					stack = new StackPanel();

				height = 0;

				foreach (var subItem in item.Items)
				{
					var subItemVisual = ConstructVisual(subItem, pixelWidth, out var subItemHeight, dryRun);

					if (subItemVisual != null)
						stack!.Children.Add(subItemVisual);

					height += subItemHeight;
				}

				return stack;
			}
			case RenderPlanItemType.GridRow:
			{
				Grid? grid = null;

				if (!dryRun)
					grid = new Grid();

				height = 0;

				for (int columnIndex = 0; columnIndex < item.Items.Count; columnIndex++)
				{
					var subItem = item.Items[columnIndex];

					int subPixelWidth = item.GridParameters?.GetColumnPixelWidth(columnIndex, pixelWidth) ?? (pixelWidth / item.Items.Count);

					var subItemVisual = ConstructVisual(subItem, subPixelWidth, out var subItemHeight, dryRun);

					if (subItemVisual != null)
					{
						grid!.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(subPixelWidth) });
						grid.Children.Add(subItemVisual);

						switch (item.GridParameters?.GetColumnAlignment(columnIndex))
						{
							case AlignmentX.Center: subItemVisual.HorizontalAlignment = HorizontalAlignment.Center; break;
							case AlignmentX.Right: subItemVisual.HorizontalAlignment = HorizontalAlignment.Right; break;
						}

						Grid.SetColumn(subItemVisual, columnIndex);
					}

					height = Math.Max(height, subItemHeight);
				}

				if (grid != null)
				{
					grid.Width = pixelWidth;
					grid.Height = height;
				}

				return grid;
			}
		}

		return null;
	}
}