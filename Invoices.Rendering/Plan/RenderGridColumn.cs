using System.Windows.Media;

namespace Invoices.Rendering.Plan;

using Invoices.Rendering.Text;

public class RenderGridColumn
{
	public int PixelWidth;
	public AlignmentX Alignment;
	public RenderFont? Font;

	public static RenderGridColumn Default => new RenderGridColumn();

	public static RenderGridColumn ForWidth(int width) =>
		new RenderGridColumn()
		{
			PixelWidth = width
		};
}
