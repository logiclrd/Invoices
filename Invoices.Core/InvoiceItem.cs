using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Invoices.Core;

public class InvoiceItem : INotifyPropertyChanged
{
	string _description;
	int _quantity;
	decimal _unitPrice;

	public string? Description
	{
		get => _description;
		set
		{
			_description = value;
			OnPropertyChanged();
		}
	}

	public int Quantity
	{
		get => _quantity;
		set
		{
			_quantity = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(LineTotal));
		}
	}

	public decimal UnitPrice
	{
		get => _unitPrice;
		set
		{
			_unitPrice = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(LineTotal));
		}
	}

	public decimal LineTotal => Quantity * UnitPrice;

	public static InvoiceItem Rehydrate((int InvoiceID, int Sequence, string Description, int Quantity, decimal UnitPrice) data)
		=> Rehydrate(data.Description, data.Quantity, data.UnitPrice);

	public static InvoiceItem Rehydrate(string description, int quantity, decimal unitPrice)
	{
		return
			new InvoiceItem()
			{
				Description = description,
				Quantity = quantity,
				UnitPrice = unitPrice,
			};
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}