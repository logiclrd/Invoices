using System.IO;

namespace Invoices.Rendering;

public class Assets
{
	static readonly string s_AssetDirectoryPath;

	static Assets()
	{
		s_AssetDirectoryPath = Path.GetDirectoryName(typeof(InvoiceRenderer).Assembly.Location)!;
	}

	public static string GetPath(string assetName)
		=> Path.Combine(s_AssetDirectoryPath, assetName);
}
