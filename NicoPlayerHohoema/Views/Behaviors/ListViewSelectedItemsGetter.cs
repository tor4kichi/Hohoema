using Microsoft.Xaml.Interactivity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace NicoPlayerHohoema.Views.Behaviors
{
	public class ListViewSelectedItemsGetter : Behavior<ListView>
	{
		public IList SelectedItems
		{
			get { return (IList)GetValue(SelectedItemsProperty); }
			set { SetValue(SelectedItemsProperty, value); }
		}


		// Using a DependencyProperty as the backing store for WorkItems.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty SelectedItemsProperty =
			DependencyProperty.Register("SelectedItems"
				, typeof(IList)
				, typeof(ListViewSelectedItemsGetter)
				, new PropertyMetadata(null)
				);
			



		protected override void OnAttached()
		{
			base.OnAttached();

			this.AssociatedObject.SelectionChanged += AssociatedObject_SelectionChanged;
		}

		
		protected override void OnDetaching()
		{
			base.OnDetaching();

			this.AssociatedObject.SelectionChanged -= AssociatedObject_SelectionChanged;
		}

		private void AssociatedObject_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var me = sender as ListView;

			if (SelectedItems != null)
			{
				SelectedItems.Clear();

				foreach (var item in me.SelectedItems)
				{
					SelectedItems.Add(item);
				}
			}
		}

	}
}
