using Microsoft.Xaml.Interactivity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace NicoPlayerHohoema.Views.Behaviors
{
	public class ListViewSelectedItemsGetter : Behavior<ListViewBase>
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
				, new PropertyMetadata(null, OnSelectedItemsPropertyChanged)
				);


		public static void OnSelectedItemsPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
		{
			var source = (ListViewSelectedItemsGetter)sender;

			if (args.OldValue != null)
			{
				source.RemoveNotifyCollectionChangedHandler(args.OldValue);
			}

			if (args.NewValue != null)
			{
				source.AddNotifyCollectionChangedHandler(args.NewValue);
			}
		}


		private void RemoveNotifyCollectionChangedHandler(object item)
		{
			if (item is INotifyCollectionChanged)
			{
				var collection = item as INotifyCollectionChanged;

				collection.CollectionChanged -= this.Collection_CollectionChanged;
			}
		}

		private void AddNotifyCollectionChangedHandler(object item)
		{
			if (item is INotifyCollectionChanged)
			{
				var collection = item as INotifyCollectionChanged;

				collection.CollectionChanged += this.Collection_CollectionChanged;
			}
		}


		private void Collection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			// ListViewの選択状態をコレクションアイテムの状態に合わせて変更する
			if(e.Action == NotifyCollectionChangedAction.Remove)
			{
				var rawListItems = this.AssociatedObject.Items
					.Select(x =>
					{
						return this.AssociatedObject.ContainerFromItem(x) as ListViewItem;
					})
					.Where(x => x != null)
					.ToList();

				var removeItems = e.OldItems.Cast<object>();

				var removeTargets = removeItems.Select(x =>
				{
					return rawListItems.SingleOrDefault(y => x == y.Content);
				})
				.ToArray();

				foreach (var i in removeTargets)
				{
					if (i != null)
					{
						i.IsSelected = false;
					}
				}
			}
			else if (e.Action == NotifyCollectionChangedAction.Reset)
			{
				this.AssociatedObject.SelectedIndex = -1;
				this.AssociatedObject.UpdateLayout();
			}
		}

		protected override void OnAttached()
		{
			base.OnAttached();

			this.AssociatedObject.SelectionChanged += AssociatedObject_SelectionChanged;

            this.AssociatedObject.Loaded += AssociatedObject_Loaded;
        }

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            if (SelectedItems != null)
            {
                Array selecteItems = Array.CreateInstance(typeof(object), SelectedItems.Count);
                SelectedItems.CopyTo(selecteItems, 0);
                for (var i = 0; i < selecteItems.Length; i++)
                {
                    var item = selecteItems.GetValue(i);
                    this.AssociatedObject.SelectedItems.Add(item);
                }
            }
        }

        protected override void OnDetaching()
		{
			base.OnDetaching();

			this.AssociatedObject.SelectionChanged -= AssociatedObject_SelectionChanged;


			RemoveNotifyCollectionChangedHandler(SelectedItems);
		}

		private void AssociatedObject_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var me = sender as ListViewBase;

            if (SelectedItems != null)
			{
				// Viewで追加済みのアイテムをVM側のコレクションに追加
				foreach (var item in me.SelectedItems)
				{
					var alreadyAdded = false;
					foreach (var selectedItem in SelectedItems)
					{
						if (item == selectedItem)
						{
							alreadyAdded = true;
							break;
						}
					}

					if (!alreadyAdded)
					{
						SelectedItems.Add(item);
					}
				}

				// Viewに存在しないVM側のアイテムをVM側のコレクションから削除
				object[] selectedItems = new object[SelectedItems.Count];
				SelectedItems.CopyTo(selectedItems, 0);
				foreach (var item in selectedItems)
				{
					if (me.SelectedItems.All(x => x != item))
					{
						SelectedItems.Remove(item);
					}
				}
			}
		}

	}
}
