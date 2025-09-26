using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Invoices.Rendering;

public class ImageLoader
{
	public static BitmapSource LoadImage(string? path)
	{
		if (path == null)
			return BitmapSource.Create(0, 0, 300, 300, PixelFormats.Pbgra32, null, new int[0], 0);

		using (var stream = File.OpenRead(path))
		{
			var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);

			return decoder.Frames[0];
		}
	}
}