using System.Collections.Generic;

namespace Invoices.Rendering.Utility;

public class StringUtility
{
	public static IEnumerable<string> WordWrap(string text, int characterWidth)
	{
		int index = 0;

		int lineStart = 0;
		int lineEnd = 0;

		while (index < text.Length)
		{
			int lastWordEnd = index;

			while ((lastWordEnd < text.Length) && !char.IsWhiteSpace(text, lastWordEnd))
				lastWordEnd++;

			int nextWordStart = lastWordEnd;

			while ((nextWordStart < text.Length) && char.IsWhiteSpace(text, nextWordStart))
				nextWordStart++;

			int newLineLength = lastWordEnd - lineStart;

			if (newLineLength > characterWidth)
			{
				yield return text.Substring(lineStart, lineEnd - lineStart);

				lineStart = index;

				while ((lineStart < text.Length) && char.IsWhiteSpace(text, lineStart))
					lineStart++;
			}
			else
				lineEnd = lastWordEnd;

			index = nextWordStart;
		}

		while ((lineStart < text.Length) && char.IsWhiteSpace(text, lineStart))
			lineStart++;

		if (lineStart < text.Length)
			yield return text.Substring(lineStart);
	}
}
