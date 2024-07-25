using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using Microsoft.Data.SqlClient;

namespace Invoices;

public class Database : IDisposable
{
	SqlConnection _connection;

	public Database()
	{
		_connection = new SqlConnection();

		_connection.ConnectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=Invoices;Integrated Security=true;TrustServerCertificate=true";
		_connection.Open();
	}

	public void Dispose()
	{
		_connection.Dispose();
	}

	TaxDefinition ReadTaxDefinition(SqlDataReader reader)
	{
		int taxID_ordinal = reader.GetOrdinal("TaxID");
		int taxName_ordinal = reader.GetOrdinal("TaxName");
		int taxRate_ordinal = reader.GetOrdinal("TaxRate");

		var taxDefinition = new TaxDefinition();

		taxDefinition.TaxID = reader.GetInt32(taxID_ordinal);
		taxDefinition.TaxName = reader.GetString(taxName_ordinal);
		taxDefinition.TaxRate = reader.GetDecimal(taxRate_ordinal);

		return taxDefinition;
	}

	public TaxDefinition GetOrCreateTaxDefinition(Tax tax)
	{
		using (var cmd = _connection.CreateCommand())
		{
			cmd.CommandText = "SELECT TaxID, TaxName, TaxRate FROM Taxes WHERE TaxName = @TaxName";

			cmd.Parameters.Add("@TaxName", SqlDbType.NVarChar).Value = tax.TaxName;

			using (var reader = cmd.ExecuteReader())
			{
				if (reader.Read())
					return ReadTaxDefinition(reader);
			}

			cmd.CommandText = "INSERT INTO Taxes (TaxName, TaxRate) OUTPUT INSERTED.TaxID, INSERTED.TaxName, INSERTED.TaxRate VALUES (@TaxName, @TaxRate)";

			cmd.Parameters.Add("@TaxRate", SqlDbType.Decimal).Value = tax.TaxRate;

			using (var reader = cmd.ExecuteReader())
			{
				if (!reader.Read())
					throw new Exception("Sanity failure: No data from OUTPUT clause");

				return ReadTaxDefinition(cmd.ExecuteReader());
			}
		}
	}

	public void SaveInvoice(Invoice invoice)
	{
		SaveInvoice(invoice, LoadTaxDefinitions());
	}

	public void SaveInvoice(Invoice invoice, Dictionary<int, TaxDefinition> taxDefinitions)
	{
		var taxDefinitionByName = taxDefinitions.Values.ToDictionary(definition => definition.TaxName ?? "", StringComparer.InvariantCultureIgnoreCase);

		DeleteInvoice(invoice.InvoiceNumber);

		using (var cmd = _connection.CreateCommand())
		{
			int invoiceID = -1;

			void InsertInvoices()
			{
				cmd.CommandText = "INSERT INTO Invoices (InvoiceNumber, InvoiceDate, InvoiceStateID, InvoiceStateDescription, PayableTo, ProjectName, DueDate) OUTPUT (INSERTED.InvoiceID) VALUES (@InvoiceNumber, @InvoiceDate, @InvoiceStateID, @InvoiceStateDescription, @PayableTo, @ProjectName, @DueDate)";

				cmd.Parameters.Add("@InvoiceNumber", SqlDbType.NVarChar, 10).Value = invoice.InvoiceNumber;
				cmd.Parameters.Add("@InvoiceDate", SqlDbType.DateTime2).Value = invoice.InvoiceDate;
				cmd.Parameters.Add("@InvoiceStateID", SqlDbType.Int).Value = (int)invoice.State;
				cmd.Parameters.Add("@InvoiceStateDescription", SqlDbType.NVarChar, 250).Value = invoice.StateDescription;
				cmd.Parameters.Add("@PayableTo", SqlDbType.NVarChar, 250).Value = invoice.PayableTo;
				cmd.Parameters.Add("@ProjectName", SqlDbType.NVarChar, 250).Value = invoice.ProjectName;
				cmd.Parameters.Add("@DueDate", SqlDbType.DateTime2).Value = invoice.DueDate;

				invoiceID = (int)cmd.ExecuteScalar();

				cmd.Parameters.Clear();
			}

			void InsertInvoiceRelations()
			{
				cmd.CommandText = "INSERT INTO InvoiceRelations (InvoiceID, RelationTypeID, ReferencesInvoiceID) VALUES (@InvoiceID, @RelationTypeID, @ReferencesInvoiceID)";

				var invoiceIDParam = cmd.Parameters.Add("@InvoiceID", SqlDbType.Int);
				var relationTypeIDParam = cmd.Parameters.Add("@RelationTypeID", SqlDbType.Int);
				var referencesInvoiceIDParam = cmd.Parameters.Add("@ReferencesInvoiceID", SqlDbType.Int);

				relationTypeIDParam.Value = (int)InvoiceRelationType.Predecessor;

				invoiceIDParam.Value = invoiceID;
				foreach (var predecessorInvoiceID in invoice.PredecessorInvoiceIDs)
				{
					referencesInvoiceIDParam.Value = predecessorInvoiceID;
					cmd.ExecuteNonQuery();
				}

				referencesInvoiceIDParam.Value = invoiceID;
				foreach (var successorInvoiceID in invoice.SuccessorInvoiceIDs)
				{
					invoiceIDParam.Value = successorInvoiceID;
					cmd.ExecuteNonQuery();
				}

				relationTypeIDParam.Value = (int)InvoiceRelationType.Successor;

				invoiceIDParam.Value = invoiceID;
				foreach (var successorInvoiceID in invoice.SuccessorInvoiceIDs)
				{
					referencesInvoiceIDParam.Value = successorInvoiceID;
					cmd.ExecuteNonQuery();
				}

				referencesInvoiceIDParam.Value = invoiceID;
				foreach (var predecessorInvoiceID in invoice.PredecessorInvoiceIDs)
				{
					invoiceIDParam.Value = predecessorInvoiceID;
					cmd.ExecuteNonQuery();
				}

				relationTypeIDParam.Value = (int)InvoiceRelationType.Related;

				foreach (var relatedInvoiceID in invoice.RelatedInvoiceIDs)
				{
					invoiceIDParam.Value = invoiceID;
					referencesInvoiceIDParam.Value = relatedInvoiceID;

					cmd.ExecuteNonQuery();

					invoiceIDParam.Value = relatedInvoiceID;
					referencesInvoiceIDParam.Value = invoiceID;

					cmd.ExecuteNonQuery();
				}

				cmd.Parameters.Clear();
			}

			void InsertInvoiceInvoicees()
			{
				cmd.CommandText = "INSERT INTO InvoiceInvoicees (InvoiceID, LineNumber, InvoiceeLine) VALUES (@InvoiceID, @LineNumber, @InvoiceeLine)";

				cmd.Parameters.Add("@InvoiceID", SqlDbType.Int).Value = invoiceID;

				var lineNumberParam = cmd.Parameters.Add("@LineNumber", SqlDbType.Int);
				var invoiceeLineParam = cmd.Parameters.Add("@InvoiceeLine", SqlDbType.NVarChar);

				for (int i=0; i < invoice.Invoicee.Count; i++)
				{
					lineNumberParam.Value = i;
					invoiceeLineParam.Value = invoice.Invoicee[i];

					cmd.ExecuteNonQuery();
				}

				cmd.Parameters.Clear();
			}

			void InsertInvoiceItems()
			{
				cmd.CommandText = "INSERT INTO InvoiceItems (InvoiceID, Sequence, Description, Quantity, UnitPrice) VALUES (@InvoiceID, @Sequence, @Description, @Quantity, @UnitPrice)";

				cmd.Parameters.Add("@InvoiceID", SqlDbType.Int).Value = invoiceID;

				var sequenceParam = cmd.Parameters.Add("@Sequence", SqlDbType.Int);
				var descriptionParam = cmd.Parameters.Add("@Description", SqlDbType.NVarChar);
				var quantityParam = cmd.Parameters.Add("@Quantity", SqlDbType.Int);
				var unitPriceParam = cmd.Parameters.Add("@UnitPrice", SqlDbType.Decimal);

				for (int i=0; i < invoice.Items.Count; i++)
				{
					var item = invoice.Items[i];

					sequenceParam.Value = i;
					descriptionParam.Value = item.Description;
					quantityParam.Value = item.Quantity;
					unitPriceParam.Value = item.UnitPrice;

					cmd.ExecuteNonQuery();
				}

				cmd.Parameters.Clear();
			}

			void InsertInvoiceTaxes()
			{
				cmd.CommandText = "INSERT INTO InvoiceTaxes (InvoiceID, Sequence, TaxID) VALUES (@InvoiceID, @Sequence, @TaxID)";

				cmd.Parameters.Add("@InvoiceID", SqlDbType.Int).Value = invoiceID;

				var sequenceParam = cmd.Parameters.Add("@Sequence", SqlDbType.Int);
				var taxIDParam = cmd.Parameters.Add("@TaxID", SqlDbType.Int);

				for (int i=0; i < invoice.Taxes.Count; i++)
				{
					var tax = invoice.Taxes[i];

					if (tax.TaxID == null)
					{
						if (tax.TaxName == null)
							throw new Exception("Tax is not properly specified (no TaxID or TaxName)");

						if (!taxDefinitionByName.TryGetValue(tax.TaxName, out var taxDefinition))
						{
							taxDefinition = GetOrCreateTaxDefinition(tax);

							if (taxDefinition.TaxRate != tax.TaxRate)
								throw new Exception("Defined tax '" + taxDefinition.TaxName + "' has a different rate (" + taxDefinition.TaxRate + ") than tax '" + tax.TaxName + "' in the invoice (" + tax.TaxRate + ")");
						}

						tax.TaxID = taxDefinition.TaxID;
						tax.TaxRate = taxDefinition.TaxRate;
					}

					sequenceParam.Value = i;
					taxIDParam.Value = tax.TaxID;
				}

				cmd.Parameters.Clear();
			}

			void InsertInvoicePayments()
			{
				cmd.CommandText = "INSERT INTO InvoicePayments (InvoiceID, Sequence, PaymentTypeID, PaymentTypeCustom, ReceivedDateTime, Amount) VALUES (@InvoiceID, @Sequence, @PaymentTypeID, @PaymentTypeCustom, @ReceivedDateTime, @Amount)";

				cmd.Parameters.Add("@InvoiceID", SqlDbType.Int).Value = invoiceID;

				var sequenceParam = cmd.Parameters.Add("@Sequence", SqlDbType.Int);
				var paymentTypeIDParam = cmd.Parameters.Add("@PaymentTypeID", SqlDbType.Int);
				var paymentTypeCustomParam = cmd.Parameters.Add("@PaymentTypeCustom", SqlDbType.NVarChar);
				var receivedDateTimeParam = cmd.Parameters.Add("@ReceivedDateTime", SqlDbType.DateTime2);
				var amountParam = cmd.Parameters.Add("@Amount", SqlDbType.Decimal);

				for (int i=0; i < invoice.Payments.Count; i++)
				{
					var payment = invoice.Payments[i];

					sequenceParam.Value = i;
					paymentTypeIDParam.Value = (int)payment.PaymentType;
					paymentTypeCustomParam.Value = payment.PaymentTypeCustom ?? (object)DBNull.Value;
					receivedDateTimeParam.Value = payment.ReceivedDateTime;
					amountParam.Value = payment.Amount;

					cmd.ExecuteNonQuery();
				}

				cmd.Parameters.Clear();
			}

			void InsertInvoiceNotes()
			{
				cmd.CommandText = "INSERT INTO InvoiceNotes (InvoiceID, Sequence, TextLine) VALUES (@InvoiceID, @Sequence, @TextLine)";

				cmd.Parameters.Add("@InvoiceID", SqlDbType.Int).Value = invoiceID;

				var sequenceParam = cmd.Parameters.Add("@Sequence", SqlDbType.Int);
				var textLineParam = cmd.Parameters.Add("@TextLine", SqlDbType.NVarChar);

				for (int i=0; i < invoice.Notes.Count; i++)
				{
					sequenceParam.Value = i;
					textLineParam.Value = invoice.Notes[i];

					cmd.ExecuteNonQuery();
				}
			}

			InsertInvoices();

			invoice.InvoiceID = invoiceID;

			InsertInvoiceRelations();
			InsertInvoiceInvoicees();
			InsertInvoiceItems();
			InsertInvoiceTaxes();
			InsertInvoicePayments();
			InsertInvoiceNotes();
		}
	}

	public Dictionary<int, TaxDefinition> LoadTaxDefinitions()
	{
		using (var cmd = _connection.CreateCommand())
		{
			cmd.CommandText = "SELECT TaxID, TaxName, TaxRate FROM Taxes";

			var lookUpTable = new Dictionary<int, TaxDefinition>();

			using (var reader = cmd.ExecuteReader())
			{
				while (reader.Read())
				{
					var taxDefinition = ReadTaxDefinition(reader);

					lookUpTable[taxDefinition.TaxID] = taxDefinition;
				}
			}

			return lookUpTable;
		}
	}

	public int GetInvoiceIDByInvoiceNumber(string invoiceNumber)
	{
		using (var cmd = _connection.CreateCommand())
		{
			cmd.CommandText = "SELECT InvoiceID FROM Invoices WHERE InvoiceNumber = @InvoiceNumber";

			cmd.Parameters.Add("@InvoiceNumber", SqlDbType.NVarChar).Value = invoiceNumber;

			if (cmd.ExecuteScalar() is int invoiceID)
				return invoiceID;
			else
				throw new KeyNotFoundException();
		}
	}

	public Invoice LoadInvoice(int invoiceID)
	{
		var taxDefinitions = LoadTaxDefinitions();

		return LoadInvoice(invoiceID, taxDefinitions);
	}

	public Invoice LoadInvoice(int invoiceID, Dictionary<int, TaxDefinition> taxDefinitions)
	{
		Console.WriteLine("LoadInvoice({0})", invoiceID);

		using (var cmd = _connection.CreateCommand())
		{
			cmd.CommandText = @"
SELECT * FROM InvoiceRelations WHERE InvoiceID = @InvoiceID;
SELECT * FROM InvoiceInvoicees WHERE InvoiceID = @InvoiceID;
SELECT * FROM InvoiceItems WHERE InvoiceID = @InvoiceID;
SELECT * FROM InvoiceTaxes WHERE InvoiceID = @InvoiceID;
SELECT * FROM InvoicePayments WHERE InvoiceID = @InvoiceID;
SELECT * FROM InvoiceNotes WHERE InvoiceID = @InvoiceID;
SELECT * FROM Invoices WHERE InvoiceID = @InvoiceID";

			cmd.Parameters.Add("@InvoiceID", SqlDbType.Int).Value = invoiceID;

			using (var reader = cmd.ExecuteReader())
				return LoadInvoices(reader, taxDefinitions).Single();
		}
	}

	public Invoice LoadInvoice(string invoiceNumber)
	{
		var taxDefinitions = LoadTaxDefinitions();

		return LoadInvoice(invoiceNumber, taxDefinitions);
	}

	public Invoice LoadInvoice(string invoiceNumber, Dictionary<int, TaxDefinition> taxDefinitions)
	{
		return LoadInvoice(GetInvoiceIDByInvoiceNumber(invoiceNumber));
	}

	IEnumerable<(int InvoiceID, InvoiceRelationType RelationType, int ReferencesInvoiceID)> ReadInvoiceRelations(SqlDataReader reader)
	{
		int invoiceID_ordinal = reader.GetOrdinal("InvoiceID");
		int relationTypeID_ordinal = reader.GetOrdinal("RelationTypeID");
		int referencesInvoiceID_ordinal = reader.GetOrdinal("ReferencesInvoiceID");

		while (reader.Read())
		{
			Console.WriteLine("InvoiceRelations");

			int invoiceID = reader.GetInt32(invoiceID_ordinal);
			InvoiceRelationType relationType = (InvoiceRelationType)reader.GetInt32(relationTypeID_ordinal);
			int referencesInvoiceID = reader.GetInt32(referencesInvoiceID_ordinal);

			yield return (invoiceID, relationType, referencesInvoiceID);
		}
	}

	IEnumerable<(int InvoiceID, int LineNumber, string InvoiceeLine)> ReadInvoiceInvoicees(SqlDataReader reader)
	{
		int invoiceID_ordinal = reader.GetOrdinal("InvoiceID");
		int lineNumber_ordinal = reader.GetOrdinal("LineNumber");
		int invoiceeLine_ordinal = reader.GetOrdinal("InvoiceeLine");

		while (reader.Read())
		{
			Console.WriteLine("InvoiceInvoicees");

			int invoiceID = reader.GetInt32(invoiceID_ordinal);
			int lineNumber = reader.GetInt32(lineNumber_ordinal);
			string invoiceeLine = reader.GetString(invoiceeLine_ordinal);

			yield return (invoiceID, lineNumber, invoiceeLine);
		}
	}

	IEnumerable<(int InvoiceID, int Sequence, string Description, int Quantity, decimal UnitPrice)> ReadInvoiceItems(SqlDataReader reader)
	{
		int invoiceID_ordinal = reader.GetOrdinal("InvoiceID");
		int sequence_ordinal = reader.GetOrdinal("Sequence");
		int description_ordinal = reader.GetOrdinal("Description");
		int quantity_ordinal = reader.GetOrdinal("Quantity");
		int unitPrice_ordinal = reader.GetOrdinal("UnitPrice");

		while (reader.Read())
		{
			Console.WriteLine("InvoiceItems");

			int invoiceID = reader.GetInt32(invoiceID_ordinal);
			int sequence = reader.GetInt32(sequence_ordinal);
			string description = reader.GetString(description_ordinal);
			int quantity = reader.GetInt32(quantity_ordinal);
			decimal unitPrice = reader.GetDecimal(unitPrice_ordinal);

			yield return (invoiceID, sequence, description, quantity, unitPrice);
		}
	}

	IEnumerable<(int InvoiceID, int Sequence, int TaxID)> ReadInvoiceTaxes(SqlDataReader reader)
	{
		int invoiceID_ordinal = reader.GetOrdinal("InvoiceID");
		int sequence_ordinal = reader.GetOrdinal("Sequence");
		int taxID_ordinal = reader.GetOrdinal("TaxID");

		while (reader.Read())
		{
			Console.WriteLine("InvoiceTaxes");

			int invoiceID = reader.GetInt32(invoiceID_ordinal);
			int sequence = reader.GetInt32(sequence_ordinal);
			int taxID = reader.GetInt32(taxID_ordinal);
			
			yield return (invoiceID, sequence, taxID);
		}
	}

	IEnumerable<(int InvoiceID, int Sequence, PaymentType PaymentType, string? PaymentTypeCustom, DateTime? ReceivedDateTime, decimal Amount)> ReadInvoicePayments(SqlDataReader reader)
	{
		int invoiceID_ordinal = reader.GetOrdinal("InvoiceID");
		int sequence_ordinal = reader.GetOrdinal("Sequence");
		int paymentTypeID_ordinal = reader.GetOrdinal("PaymentTypeID");
		int paymentTypeCustom_ordinal = reader.GetOrdinal("PaymentTypeCustom");
		int receivedDateTime_ordinal = reader.GetOrdinal("ReceivedDateTime");
		int amount_ordinal = reader.GetOrdinal("Amount");

		while (reader.Read())
		{
			Console.WriteLine("InvoicePayments");

			int invoiceID = reader.GetInt32(invoiceID_ordinal);
			int sequence = reader.GetInt32(sequence_ordinal);
			int paymentTypeID = reader.GetInt32(paymentTypeID_ordinal);
			string? paymentTypeCustom = reader.IsDBNull(paymentTypeCustom_ordinal) ? default : reader.GetString(paymentTypeCustom_ordinal);
			DateTime? receivedDateTime = reader.IsDBNull(receivedDateTime_ordinal) ? default : reader.GetDateTime(receivedDateTime_ordinal);
			decimal amount = reader.GetDecimal(amount_ordinal);
			
			yield return (invoiceID, sequence, (PaymentType)paymentTypeID, paymentTypeCustom, receivedDateTime, amount);
		}
	}

	IEnumerable<(int InvoiceID, int Sequence, string TextLine)> ReadInvoiceNotes(SqlDataReader reader)
	{
		int invoiceID_ordinal = reader.GetOrdinal("InvoiceID");
		int sequence_ordinal = reader.GetOrdinal("Sequence");
		int textLine_ordinal = reader.GetOrdinal("TextLine");

		while (reader.Read())
		{
			Console.WriteLine("InvoiceNotes");

			int invoiceID = reader.GetInt32(invoiceID_ordinal);
			int sequence = reader.GetInt32(sequence_ordinal);
			string textLine = reader.GetString(textLine_ordinal);

			yield return (invoiceID, sequence, textLine);
		}
	}

	IEnumerable<Invoice> ReadInvoices(SqlDataReader reader)
	{
		int invoiceID_ordinal = reader.GetOrdinal("InvoiceID");
		int invoiceNumber_ordinal = reader.GetOrdinal("InvoiceNumber");
		int invoiceDate_ordinal = reader.GetOrdinal("InvoiceDate");
		int invoiceStateID_ordinal = reader.GetOrdinal("InvoiceStateID");
		int invoiceStateDescription_ordinal = reader.GetOrdinal("InvoiceStateDescription");
		int payableTo_ordinal = reader.GetOrdinal("PayableTo");
		int projectName_ordinal = reader.GetOrdinal("ProjectName");
		int dueDate_ordinal = reader.GetOrdinal("DueDate");

		while (reader.Read())
		{
			Console.WriteLine("Invoices");

			int invoiceID = reader.GetInt32(invoiceID_ordinal);
			string invoiceNumber = reader.GetString(invoiceNumber_ordinal);
			DateTime invoiceDate = reader.GetDateTime(invoiceDate_ordinal);
			InvoiceState state = (InvoiceState)reader.GetInt32(invoiceStateID_ordinal);
			string stateDescription = reader.GetString(invoiceStateDescription_ordinal);
			string payableTo = reader.GetString(payableTo_ordinal);
			string projectName = reader.GetString(projectName_ordinal);
			DateTime? dueDate = reader.IsDBNull(dueDate_ordinal) ? default : reader.GetDateTime(dueDate_ordinal);

			yield return
				new Invoice()
				{
					InvoiceID = invoiceID,
					InvoiceNumber = invoiceNumber,
					InvoiceDate = invoiceDate,
					State = state,
					StateDescription = stateDescription,
					PayableTo = payableTo,
					ProjectName = projectName,
					DueDate = dueDate,
				};
		}
	}

	IEnumerable<Invoice> LoadInvoices(SqlDataReader reader, Dictionary<int, TaxDefinition> taxDefinitions)
	{
		var relationsByInvoiceID = ReadInvoiceRelations(reader).GroupBy(relation => relation.InvoiceID).ToDictionary(grouping => grouping.Key, grouping => grouping.AsEnumerable());

		if (!reader.NextResult())
			yield break;

		var invoiceesByInvoiceID = ReadInvoiceInvoicees(reader).GroupBy(invoicee => invoicee.InvoiceID).ToDictionary(grouping => grouping.Key, grouping => grouping.AsEnumerable());

		if (!reader.NextResult())
			yield break;

		var itemsByInvoiceID = ReadInvoiceItems(reader).GroupBy(item => item.InvoiceID).ToDictionary(grouping => grouping.Key, grouping => grouping.AsEnumerable());

		if (!reader.NextResult())
			yield break;

		var taxesByInvoiceID = ReadInvoiceTaxes(reader).GroupBy(tax => tax.InvoiceID).ToDictionary(grouping => grouping.Key, grouping => grouping.AsEnumerable());

		if (!reader.NextResult())
			yield break;

		var paymentsByInvoiceID = ReadInvoicePayments(reader).GroupBy(payment => payment.InvoiceID).ToDictionary(grouping => grouping.Key, grouping => grouping.AsEnumerable());

		if (!reader.NextResult())
			yield break;

		var notesByInvoiceID = ReadInvoiceNotes(reader).GroupBy(note => note.InvoiceID).ToDictionary(grouping => grouping.Key, grouping => grouping.AsEnumerable());

		if (!reader.NextResult())
			yield break;

		foreach (var invoice in ReadInvoices(reader))
		{
			if (relationsByInvoiceID.TryGetValue(invoice.InvoiceID, out var relations))
			{
				invoice.PredecessorInvoiceIDs = relations.Where(relation => relation.RelationType == InvoiceRelationType.Predecessor).Select(relation => relation.ReferencesInvoiceID).ToList();
				invoice.SuccessorInvoiceIDs = relations.Where(relation => relation.RelationType == InvoiceRelationType.Successor).Select(relation => relation.ReferencesInvoiceID).ToList();
				invoice.RelatedInvoiceIDs = relations.Where(relation => relation.RelationType == InvoiceRelationType.Related).Select(relation => relation.ReferencesInvoiceID).ToList();
			}

			if (invoiceesByInvoiceID.TryGetValue(invoice.InvoiceID, out var invoiceeLines))
				invoice.Invoicee = invoiceeLines.OrderBy(line => line.LineNumber).Select(line => line.InvoiceeLine).ToList();

			if (itemsByInvoiceID.TryGetValue(invoice.InvoiceID, out var items))
				invoice.Items = items.OrderBy(item => item.Sequence).Select(InvoiceItem.Rehydrate).ToList();

			if (taxesByInvoiceID.TryGetValue(invoice.InvoiceID, out var taxes))
			{
				TaxDefinition? taxDefinition = null;

				invoice.Taxes = taxes.OrderBy(tax => tax.Sequence).Where(data => taxDefinitions.TryGetValue(data.TaxID, out taxDefinition)).Select(data => Tax.Rehydrate(taxDefinition!)).ToList();
			}

			if (paymentsByInvoiceID.TryGetValue(invoice.InvoiceID, out var payments))
				invoice.Payments = payments.OrderBy(payment => payment.Sequence).Select(Payment.Rehydrate).ToList();

			if (notesByInvoiceID.TryGetValue(invoice.InvoiceID, out var notes))
				invoice.Notes = notes.OrderBy(note => note.Sequence).Select(note => note.TextLine).ToList();

			yield return invoice;
		}
	}

	public void DeleteInvoice(int invoiceID)
	{
		using (var cmd = _connection.CreateCommand())
		{
			cmd.Parameters.Add("@InvoiceID", SqlDbType.Int).Value = invoiceID;

			cmd.CommandText = "DELETE FROM InvoiceRelations WHERE InvoiceID = @InvoiceID OR ReferencesInvoiceID = @InvoiceID";
			cmd.ExecuteNonQuery();
			cmd.CommandText = "DELETE FROM InvoiceInvoicees WHERE InvoiceID = @InvoiceID";
			cmd.ExecuteNonQuery();
			cmd.CommandText = "DELETE FROM InvoiceItems WHERE InvoiceID = @InvoiceID";
			cmd.ExecuteNonQuery();
			cmd.CommandText = "DELETE FROM InvoiceTaxes WHERE InvoiceID = @InvoiceID";
			cmd.ExecuteNonQuery();
			cmd.CommandText = "DELETE FROM InvoicePayments WHERE InvoiceID = @InvoiceID";
			cmd.ExecuteNonQuery();
			cmd.CommandText = "DELETE FROM InvoiceNotes WHERE InvoiceID = @InvoiceID";
			cmd.ExecuteNonQuery();

			cmd.CommandText = "DELETE FROM Invoices WHERE InvoiceID = @InvoiceID";
			cmd.ExecuteNonQuery();
		}
	}

	public void DeleteInvoice(string invoiceNumber)
	{
		using (var cmd = _connection.CreateCommand())
		{
			cmd.Parameters.Add("@InvoiceNumber", SqlDbType.NVarChar).Value = invoiceNumber;

			cmd.CommandText = "DELETE FROM InvoiceRelations WHERE InvoiceID IN (SELECT InvoiceID FROM Invoices WHERE InvoiceNumber = @InvoiceNumber) OR ReferencesInvoiceID IN (SELECT InvoiceID FROM Invoices WHERE InvoiceNumber = @InvoiceNumber)";
			cmd.ExecuteNonQuery();
			cmd.CommandText = "DELETE FROM InvoiceInvoicees WHERE InvoiceID IN (SELECT InvoiceID FROM Invoices WHERE InvoiceNumber = @InvoiceNumber)";
			cmd.ExecuteNonQuery();
			cmd.CommandText = "DELETE FROM InvoiceItems WHERE InvoiceID IN (SELECT InvoiceID FROM Invoices WHERE InvoiceNumber = @InvoiceNumber)";
			cmd.ExecuteNonQuery();
			cmd.CommandText = "DELETE FROM InvoiceTaxes WHERE InvoiceID IN (SELECT InvoiceID FROM Invoices WHERE InvoiceNumber = @InvoiceNumber)";
			cmd.ExecuteNonQuery();
			cmd.CommandText = "DELETE FROM InvoicePayments WHERE InvoiceID IN (SELECT InvoiceID FROM Invoices WHERE InvoiceNumber = @InvoiceNumber)";
			cmd.ExecuteNonQuery();
			cmd.CommandText = "DELETE FROM InvoiceNotes WHERE InvoiceID IN (SELECT InvoiceID FROM Invoices WHERE InvoiceNumber = @InvoiceNumber)";
			cmd.ExecuteNonQuery();

			cmd.CommandText = "DELETE FROM Invoices WHERE InvoiceID IN (SELECT InvoiceID FROM Invoices WHERE InvoiceNumber = @InvoiceNumber)";
			cmd.ExecuteNonQuery();
		}
	}
}