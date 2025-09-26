using System;
using System.Collections.Generic;
using System.Linq;

namespace Invoices.Rendering.Plan;

using Invoices.Rendering.Text;

public class RenderGridParameters
{
	public int PixelWidth;
	public List<RenderGridColumn> Columns = new List<RenderGridColumn>();

	public RenderGridParameters(int pixelWidth)
	{
		PixelWidth = pixelWidth;
	}

	public int GetColumnPixelWidth(int columnIndex, int targetPixelWidth)
	{
		if ((columnIndex >= 0) && (columnIndex < Columns.Count))
			return Columns[columnIndex].PixelWidth * targetPixelWidth / PixelWidth;
		else
			return 0;
	}

	public RenderFont? GetColumnFont(int columnIndex)
	{
		if ((columnIndex >= 0) && (columnIndex < Columns.Count))
			return Columns[columnIndex].Font;
		else
			return null;
	}

	public static RenderGridParameters Create(int pixelWidth, params double[] relativeWidths)
	{
		var ret = new RenderGridParameters(pixelWidth);

		int remainingPixelWidth = pixelWidth;

		for (int i = 0; i < relativeWidths.Length; i++)
		{
			if (relativeWidths[i] <= 0)
				ret.Columns.Add(RenderGridColumn.Default); // filled later
			else
			{
				int width = (int)Math.Ceiling(relativeWidths[i]);

				if (width > remainingPixelWidth)
					width = remainingPixelWidth;

				ret.Columns.Add(RenderGridColumn.ForWidth(width));

				remainingPixelWidth -= width;
			}
		}

		// Now distribute the remainder.
		if (remainingPixelWidth < 0)
			remainingPixelWidth = 0;

		pixelWidth = remainingPixelWidth;

		double totalRelativeWidth = relativeWidths.Where(v => v < 0).Sum();

		for (int i = 0; i < relativeWidths.Length; i++)
		{
			if (relativeWidths[i] > 0)
				continue;

			int width = (int)Math.Ceiling(pixelWidth * relativeWidths[i] / totalRelativeWidth);

			if (width > remainingPixelWidth)
				width = remainingPixelWidth;

			ret.Columns.Add(RenderGridColumn.ForWidth(width));

			remainingPixelWidth -= width;
		}

		return ret;
	}
}
