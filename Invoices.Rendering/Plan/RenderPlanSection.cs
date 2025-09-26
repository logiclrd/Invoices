using System.Collections.Generic;

namespace Invoices.Rendering.Plan;

public class RenderPlanSection
{
	public readonly RenderPlan Plan;

	public RenderPlanSection(RenderPlan plan)
	{
		Plan = plan;
	}

	List<RenderPlanItem> _items = new List<RenderPlanItem>();

	public IReadOnlyList<RenderPlanItem> Items => _items;

	public void AddItem(RenderPlanItem item)
	{
		item.Plan = Plan;
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
}
