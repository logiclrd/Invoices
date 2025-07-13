using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Invoices.Interface.Utility;

public class BindingListEx<T> : BindingList<T>
{
	public event EventHandler<T>? ItemRemoved;

	public BindingListEx()
	{
	}

	public BindingListEx(IList<T> list)
	: base(list)
	{
	}

  protected override void RemoveItem(int index)
	{
		ItemRemoved?.Invoke(this, this[index]);

		base.RemoveItem(index);
	}
}
