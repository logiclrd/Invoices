using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Invoices.Interface;

using Invoices.Core;

public class ItemTemplateCategory
{
	BindingList<ItemTemplate> _itemTemplates = new BindingList<ItemTemplate>();
	string? _categoryName;

	public BindingList<ItemTemplate> ItemTemplates => _itemTemplates;

	public string? CategoryName
	{
		get => _categoryName;
		set => _categoryName = value;
	}

	public static IList<ItemTemplateCategory> BuildCategories(IEnumerable<ItemTemplate> allTemplates)
	{
		SortedDictionary<string, ItemTemplateCategory> categories = new SortedDictionary<string, ItemTemplateCategory>();

		foreach (var template in allTemplates)
		{
			string categoryKey = template.Category ?? "";

			if (!categories.TryGetValue(categoryKey, out var category))
			{
				category = new ItemTemplateCategory();

				category.CategoryName = template.Category ?? "General";

				categories.Add(categoryKey, category);
			}

			category.ItemTemplates.Add(template);
		}

		return new BindingList<ItemTemplateCategory>(categories.Values.ToList());
	}
}
