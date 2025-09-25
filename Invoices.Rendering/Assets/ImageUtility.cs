using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace Invoices.Rendering;

public class ImageUtility
{
	public static void SavePng(BitmapSource image, string filePath)
	{
		var encoder = new PngBitmapEncoder();

		encoder.Frames.Add(BitmapFrame.Create(image));

		using (var outputStream = File.OpenWrite(filePath))
			encoder.Save(outputStream);
	}
}
