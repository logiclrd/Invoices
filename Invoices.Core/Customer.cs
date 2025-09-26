using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Invoices.Core;

public class Customer
{
	public int CustomerID { get; set; }
	public List<string> Name { get; set; } = new List<string>();
	public List<string> Address { get; set; } = new List<string>();
	public List<string> EmailAddresses { get; set; } = new List<string>();
	public List<string> PhoneNumbers { get; set; } = new List<string>();
	public List<string> Notes { get; set; } = new List<string>();

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

	public IEnumerable<string> LongSummaryLines
	{
		get
		{
			foreach (string name in Name)
				yield return name;

			bool needSeparator = false;

			if (Address.Any())
			{
				foreach (string addressLine in Address)
					yield return addressLine;
				needSeparator = true;
			}

			if (EmailAddresses.Any())
			{
				if (needSeparator)
					yield return "";
				foreach (string emailAddress in EmailAddresses)
					yield return emailAddress;
				needSeparator = true;
			}

			if (PhoneNumbers.Any())
			{
				if (needSeparator)
					yield return "";
				foreach (string phoneNumber in PhoneNumbers)
					yield return phoneNumber;
				needSeparator = true;
			}
		}
	}

	public string LongSummary
	{
		get
		{
			var result = new StringBuilder();

			foreach (var line in LongSummaryLines)
				result.AppendLine(line);

			return result.ToString();
		}
	}
}
