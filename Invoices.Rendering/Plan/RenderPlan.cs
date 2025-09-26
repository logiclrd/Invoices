using System.Linq;

namespace Invoices.Rendering.Plan;

using Invoices.Rendering.Text;

public class RenderPlan
{
	public RenderFont DefaultFont;

	public RenderPlanSection Header;
	public RenderPlanSection Body;

	public RenderPlan(RenderFont defaultFont)
	{
		DefaultFont = defaultFont;

		Header = new RenderPlanSection(this);
		Body = new RenderPlanSection(this);
	}

	public int MeasureHeight(int pixelWidth)
	{
		double height = 0;

		foreach (var item in Header.Items.Concat(Body.Items))
			height += item.MeasureHeight(pixelWidth);

		return (int)height;
	}
}
