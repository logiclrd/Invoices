using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Invoices.Core;

public class Customer
{
	public int CustomerID;
	public List<string> Name = new List<string>();
	public List<string> Address = new List<string>();
	public List<string> EmailAddresses = new List<string>();
	public List<string> PhoneNumbers = new List<string>();
	public List<string> Notes = new List<string>();

	public string Summary
	{
		get
		{
			var result = new StringBuilder();

			foreach (string name in Name)
			{
				if (result.Length > 0)
					result.Append(", ");
				result.Append(name);
			}

			if (Address.Any())
				result.Append(" (").Append(Address[0]).Append(")");
			else if (EmailAddresses.Any() || PhoneNumbers.Any())
				result.Append(" (").Append(string.Join(", ", EmailAddresses.Concat(PhoneNumbers))).Append(")");

			return result.ToString();
		}
	}

	public string LongSummary
	{
		get
		{
			var result = new StringBuilder();

			foreach (string name in Name)
				result.AppendLine(name);

			int finalLength = result.Length;

			if (Address.Any())
			{
				foreach (string addressLine in Address)
					result.AppendLine(addressLine);

				finalLength = result.Length;

				result.AppendLine();
			}

			if (EmailAddresses.Any())
			{
				foreach (string emailAddress in EmailAddresses)
					result.AppendLine(emailAddress);

				finalLength = result.Length;

				result.AppendLine();
			}

			if (PhoneNumbers.Any())
			{
				foreach (string phoneNumber in PhoneNumbers)
					result.AppendLine(phoneNumber);

				finalLength = result.Length;

				result.AppendLine();
			}

			result.Length = finalLength;

			return result.ToString();
		}
	}
}
