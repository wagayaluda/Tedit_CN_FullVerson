using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace TEditXna.Helper
{
    public static class Extensions
    {
        public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                collection.Add(item);
            }
        }

        public static void Replace<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
        {
            collection.Clear();
            foreach (var item in items)
            {
                collection.Add(item);
            }
        }

		public static string GetDisplayName(this Enum item)
		{
			var type = item.GetType();
			var field = type.GetField(item.ToString());
			var fieldValue = field.GetValue(null);
			var descriptions = field.GetCustomAttributes(typeof(DescriptionAttribute), true);

			string displayName;
			if (descriptions.Length > 0)
			{
				displayName = ((DescriptionAttribute)descriptions[0]).Description;
			}
			else
			{
				try
				{
					displayName = TypeDescriptor.GetConverter(type).ConvertToString(fieldValue);
				}
				catch (Exception)
				{
					displayName = field.Name;
				}
			}

			return displayName;
		}
    }
}