using System;
using System.Collections.Generic;
using System.Linq;

namespace Invoices.Rendering.Plan;

using Invoices.Rendering.Text;

public class RenderPlanItem
{
	public RenderPlan Plan = default!;
	public RenderPlanItem? Owner;

	RenderFont? _localFont;

	public RenderFont Font
	{
		get => _localFont ?? Owner?.Font ?? Plan.DefaultFont;
		set => _localFont = value;
	}

	public RenderPlanItemType ItemType;
	public RenderPlanValue? Value;
	public RenderGridParameters? GridParameters;

	List<RenderPlanItem> _items = new List<RenderPlanItem>();

	public IReadOnlyList<RenderPlanItem> Items => _items;

	public RenderPlanItem AddItem(RenderPlanItem item)
	{
		item.Plan = this.Plan;
		item.Owner = this;
		_items.Add(item);

		return this;
	}

	public RenderPlanItem AddItems(IEnumerable<RenderPlanItem> items)
	{
		foreach (var item in items)
			AddItem(item);

		return this;
	}

	public void AddGridRow(RenderGridParameters parameters, params RenderPlanItem[] cells)
	{
		var row = GridRow(parameters);

		row.AddItems(cells);

		AddItem(row);
	}

	public static RenderPlanItem Text(RenderPlanValueType type, string text) => new RenderPlanItem(new RenderPlanValue(type, text));

	public static RenderPlanItem Text(string text) => Text(RenderPlanValueType.Text, text);
	public static RenderPlanItem BoldText(string text) => Text(RenderPlanValueType.BoldText, text);
	public static RenderPlanItem TitleText(string text) => Text(RenderPlanValueType.TitleText, text);

	public static RenderPlanItem Image(string path, double? maxHeight = null) => new RenderPlanItem(RenderPlanValue.Image(path, maxHeight));

	public static RenderPlanItem Stack(params RenderPlanItem[] items) => new RenderPlanItem(RenderPlanItemType.Stack).AddItems(items);

	public static RenderPlanItem GridRow(RenderGridParameters parameters) => new RenderPlanItem(RenderPlanItemType.GridRow) { GridParameters = parameters };

	public RenderPlanItem(RenderPlanItemType itemType)
	{
		ItemType = itemType;
	}

	public RenderPlanItem(RenderPlanValue value)
	{
		ItemType = RenderPlanItemType.Value;
		Value = value;
	}

	public double MeasureHeight(int pixelWidth)
	{
		if (pixelWidth == 0)
			return 0;

		switch (ItemType)
		{
			case RenderPlanItemType.Value:
				return Value?.MeasureHeight(pixelWidth, Font) ?? 0;
			case RenderPlanItemType.Stack:
				return Items
					.Select(item => item.MeasureHeight(pixelWidth))
					.Sum();
			case RenderPlanItemType.GridRow:
				return Items
					.Select((item, columnIndex) => item.MeasureHeight(GridParameters!.GetColumnPixelWidth(columnIndex, pixelWidth)))
					.Max();

			default:
				return 0;
		}
	}
}

