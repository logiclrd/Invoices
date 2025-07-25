using System;
using System.IO;
using System.Text.Json;

namespace Invoices.Integration.Square;

public class SquareConfiguration
{
	public string? EndPoint { get; set; }
	public string? AccessToken { get; set; }

	public static SquareConfiguration LoadFrom(Stream stream)
	{
		return JsonSerializer.Deserialize<SquareConfiguration>(stream) ?? throw new FormatException();
	}

	public static SquareConfiguration LoadFrom(string path)
	{
		using (var stream = File.OpenRead(path))
			return LoadFrom(stream);
	}
}
