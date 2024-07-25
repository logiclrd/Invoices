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

		_connection.ConnectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=Invoices";
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

	public TaxDefinition GetTaxDefinition2(Tax tax)
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

			cmd.CommandText = "INSERT INTO Taxes (TaxName, TaxRate) OUTPUT (INSERTED.TaxID, INSERTED.TaxName, INSERTED.TaxRate) VALUES (@TaxName, @TaxRate)";

			cmd.Parameters.Add("@TaxRate", SqlDbType.Decimal).Value = tax.TaxRate;

			return ReadTaxDefinition(cmd.ExecuteReader());
		}
	}

	public void SaveInvoice(Invoice invoice)
	{
		SaveInvoice(invoice, LoadTaxDefinitions());
	}

	public void SaveInvoice(Invoice invoice, Dictionary<int, TaxDefinition> taxDefinitions)
	{
		var taxDefinitionByName = taxDefinitions.Values.ToDictionary(definition => definition.TaxName ?? "", StringComparer.InvariantCultureIgnoreCase);

		DeleteInvoice(invoice.InvoiceID);

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

						var taxDefinition = taxDefinitionByName[tax.TaxName];

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
					paymentTypeCustomParam.Value = payment.PaymentTypeCustom;
					receivedDateTimeParam.Value = payment.ReceivedDateTime;
					amountParam.Value = payment.Amount;

					cmd.ExecuteNonQuery();
				}

				cmd.Parameters.Clear();
			}

			InsertInvoices();
			InsertInvoiceRelations();
			InsertInvoiceInvoicees();
			InsertInvoiceItems();
			InsertInvoiceTaxes();
			InsertInvoicePayments();
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
				var taxDefinition = ReadTaxDefinition(reader);

				lookUpTable[taxDefinition.TaxID] = taxDefinition;
			}

			return lookUpTable;
		}
	}

	public Invoice LoadInvoice(int invoiceID)
	{
		var taxDefinitions = LoadTaxDefinitions();

		return LoadInvoice(invoiceID, taxDefinitions);
	}

	public Invoice LoadInvoice(int invoiceID, Dictionary<int, TaxDefinition> taxDefinitions)
	{
		using (var cmd = _connection.CreateCommand())
		{
			cmd.CommandText = @"
SELECT * FROM InvoiceRelations WHERE InvoiceID = @InvoiceID;
SELECT * FROM InvoiceInvoicees WHERE InvoiceID = @InvoiceID;
SELECT * FROM InvoiceItems WHERE InvoiceID = @InvoiceID;
SELECT * FROM InvoiceTaxes WHERE InvoiceID = @InvoiceID;
SELECT * FROM InvoicePayments WHERE InvoiceID = @InvoiceID;
SELECT * FROM Invoices WHERE InvoiceID = @InvoiceID";

			using (var reader = cmd.ExecuteReader())
				return LoadInvoices(reader, taxDefinitions).Single();
		}
	}

	IEnumerable<(int InvoiceID, InvoiceRelationType RelationType, int RelatedInvoiceID)> ReadInvoiceRelations(SqlDataReader reader)
	{
		int invoiceID_ordinal = reader.GetOrdinal("InvoiceID");
		int relationTypeID_ordinal = reader.GetOrdinal("RelationTypeID");
		int relatedInvoiceID_ordinal = reader.GetOrdinal("RelatedInvoiceID");

		while (reader.Read())
		{
			int invoiceID = reader.GetInt32(invoiceID_ordinal);
			InvoiceRelationType relationType = (InvoiceRelationType)reader.GetInt32(relationTypeID_ordinal);
			int relatedInvoiceID = reader.GetInt32(relatedInvoiceID_ordinal);

			yield return (invoiceID, relationType, relatedInvoiceID);
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
			int invoiceID = reader.GetInt32(invoiceID_ordinal);
			int sequence = reader.GetInt32(sequence_ordinal);
			int paymentTypeID = reader.GetInt32(paymentTypeID_ordinal);
			string? paymentTypeCustom = reader.IsDBNull(paymentTypeCustom_ordinal) ? default : reader.GetString(paymentTypeCustom_ordinal);
			DateTime? receivedDateTime = reader.IsDBNull(receivedDateTime_ordinal) ? default : reader.GetDateTime(receivedDateTime_ordinal);
			decimal amount = reader.GetDecimal(amount_ordinal);
			
			yield return (invoiceID, sequence, (PaymentType)paymentTypeID, paymentTypeCustom, receivedDateTime, amount);
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

		var itemsByInvoiceID = ReadInvoiceItems(reader).GroupBy(item => item.InvoiceID).ToDictionary(grouping => grouping.Key, grouping => grouping.AsEnumerable());

		if (!reader.NextResult())
			yield break;

		var taxesByInvoiceID = ReadInvoiceTaxes(reader).GroupBy(tax => tax.InvoiceID).ToDictionary(grouping => grouping.Key, grouping => grouping.AsEnumerable());

		if (!reader.NextResult())
			yield break;

		var paymentsByInvoiceID = ReadInvoicePayments(reader).GroupBy(payment => payment.InvoiceID).ToDictionary(grouping => grouping.Key, grouping => grouping.AsEnumerable());

		if (!reader.NextResult())
			yield break;

		foreach (var invoice in ReadInvoices(reader))
		{
			if (relationsByInvoiceID.TryGetValue(invoice.InvoiceID, out var relations))
			{
				invoice.PredecessorInvoiceIDs = relations.Where(relation => relation.RelationType == InvoiceRelationType.Predecessor).Select(relation => relation.RelatedInvoiceID).ToList();
				invoice.SuccessorInvoiceIDs = relations.Where(relation => relation.RelationType == InvoiceRelationType.Successor).Select(relation => relation.RelatedInvoiceID).ToList();
				invoice.RelatedInvoiceIDs = relations.Where(relation => relation.RelationType == InvoiceRelationType.Related).Select(relation => relation.RelatedInvoiceID).ToList();
			}

			if (itemsByInvoiceID.TryGetValue(invoice.InvoiceID, out var items))
				invoice.Items = items.OrderBy(item => item.Sequence).Select(InvoiceItem.Rehydrate).ToList();

			if (taxesByInvoiceID.TryGetValue(invoice.InvoiceID, out var taxes))
			{
				TaxDefinition? taxDefinition = null;

				invoice.Taxes = taxes.OrderBy(tax => tax.Sequence).Where(data => taxDefinitions.TryGetValue(data.TaxID, out taxDefinition)).Select(data => Tax.Rehydrate(taxDefinition!)).ToList();
			}

			if (paymentsByInvoiceID.TryGetValue(invoice.InvoiceID, out var payments))
				invoice.Payments = payments.OrderBy(payment => payment.Sequence).Select(Payment.Rehydrate).ToList();

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

			cmd.CommandText = "DELETE FROM Invoices WHERE InvoiceID = @InvoiceID";
			cmd.ExecuteNonQuery();
		}
	}
}