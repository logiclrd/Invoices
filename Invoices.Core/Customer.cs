using System.Collections.Generic;

namespace Invoices.Core;

public class Customer
{
	public int CustomerID;
	public List<string> Name = new List<string>();
	public List<string> Address = new List<string>();
	public List<string> EmailAddresses = new List<string>();
	public List<string> PhoneNumbers = new List<string>();
	public List<string> Notes = new List<string>();
}
