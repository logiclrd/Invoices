using System.Data;

public static class IDbDataReaderExtensions
{
	public static decimal GetSimplifiedDecimal(this IDataRecord reader, int i)
	{
		decimal value = reader.GetDecimal(i);

		string stringRepresentation = value.ToString();

		if (stringRepresentation.Length > 1)
		{
			string shorter = stringRepresentation.Substring(0, stringRepresentation.Length - 1);

			while (decimal.TryParse(shorter, out var simpler) && (value == simpler))
			{
				value = simpler;
				stringRepresentation = shorter;

				if (stringRepresentation.Length <= 1)
					break;

				shorter = stringRepresentation.Substring(0, stringRepresentation.Length - 1);
			}
		}

		return value;
	}
}