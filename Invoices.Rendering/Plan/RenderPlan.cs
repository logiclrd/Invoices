using System.Collections.Generic;

namespace Invoices.Rendering.Plan;

using Invoices.Rendering.Text;

public class RenderPlan
{
	public RenderFont DefaultFont;

	public RenderPlan(RenderFont defaultFont)
	{
		DefaultFont = defaultFont;
	}

	List<RenderPlanItem> _items = new List<RenderPlanItem>();

	public IReadOnlyList<RenderPlanItem> Items => _items;

	public void AddItem(RenderPlanItem item)
	{
		item.Plan = this;
		_items.Add(item);
	}

	public void AddItem(RenderPlanValue value)
	{
		AddItem(new RenderPlanItem(value));
	}

	public void AddGridRow(RenderGridParameters parameters, params RenderPlanItem[] cells)
	{
		var row = RenderPlanItem.GridRow(parameters);

		row.AddItems(cells);

		AddItem(row);
	}

	public int MeasureHeight(int pixelWidth)
	{
		double height = 0;

		foreach (var item in Items)
			height += item.MeasureHeight(pixelWidth);

		return (int)height;
	}
}
