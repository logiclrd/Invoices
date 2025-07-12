namespace Invoices.Interface.Utility;

public static class ObjectExtensions
{
	static object? s_dataGridPlaceholder;

	public static bool IsDataGridPlaceholder(this object? obj)
	{
		if (obj == null)
			return false;

		if (s_dataGridPlaceholder != null)
			return (obj == s_dataGridPlaceholder);

		if ((obj.GetType().Name == "NamedObject")
		 && (obj.ToString() ?? "").Contains("DataGrid.NewItemPlaceholder"))
		{
			s_dataGridPlaceholder = obj;
			return true;
		}
		else
			return false;
	}
}