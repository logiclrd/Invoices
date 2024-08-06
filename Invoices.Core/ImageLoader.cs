using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace Invoices;

public class ImageLoader
{
	public static BitmapSource LoadImage(string path)
	{
		using (var stream = File.OpenRead(path))
		{
			var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);

			return decoder.Frames[0];
		}
	}
}