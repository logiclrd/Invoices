using System.Collections.Generic;

namespace Invoices.Rendering.Plan;

public class RenderPlan
{
	public List<RenderPlanItem> Items = new List<RenderPlanItem>();

	public int MeasureHeight(int pixelWidth)
	{
		double height = 0;

		foreach (var item in Items)
			height += item.MeasureHeight(pixelWidth);

		return (int)height;
	}
}
