using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace NicoPlayerHohoema.Models.AppMap
{
	public enum ContainerItemDisplayType
	{
		Normal,
		Card,
		TwoLineText,
	}

	public interface IAppMapContainer : IAppMapItem
	{
		ReadOnlyObservableCollection<IAppMapItem> DisplayItems { get; }
		uint ItemsCount { get; }
		Windows.UI.Color ThemeColor { get; }

		ContainerItemDisplayType ItemDisplayType { get; }

		Task Refresh();
	}

	[DataContract]
	public abstract class AppMapContainerBase : IAppMapContainer
	{
		protected ObservableCollection<IAppMapItem> _DisplayItems = new ObservableCollection<IAppMapItem>();
		public ReadOnlyObservableCollection<IAppMapItem> DisplayItems { get; private set; }

		public uint ItemsCount => (uint)_DisplayItems.Count;

		[DataMember]
		public string PrimaryLabel { get; private set; }
		[DataMember]
		public string SecondaryLabel { get; protected set; }
		[DataMember]
		public HohoemaPageType PageType { get; private set; }
		[DataMember]
		public string Parameter { get; private set; }

		public virtual Windows.UI.Color ThemeColor => PageType.ToHohoemaPageTypeToDefaultColor();

		public virtual ContainerItemDisplayType ItemDisplayType => ContainerItemDisplayType.Normal;

		public AppMapContainerBase(HohoemaPageType pageType, string parameter = null, string label = null)
		{
			DisplayItems = new ReadOnlyObservableCollection<IAppMapItem>(_DisplayItems);
			PrimaryLabel = label == null ? PageManager.PageTypeToTitle(pageType) : label;
			PageType = pageType;
			Parameter = parameter;

		}

		public abstract Task Refresh();


		public async Task Reset()
		{
			_DisplayItems.Clear();

			await OnReset();
		}

		protected virtual Task OnReset()
		{
			return Task.CompletedTask;
		}

		

		protected bool EqualAppMapItem(IAppMapItem left, IAppMapItem right)
		{
			return left.PageType == right.PageType && left.Parameter == right.Parameter;
		}

		
	}

	public interface ISelectableAppMapContainer : IAppMapContainer
	{
		IReadOnlyList<IAppMapItem> SelectedItem { get; }

		IReadOnlyList<IAppMapItem> SelectableItems { get; }

		IReadOnlyList<IAppMapItem> AllItems { get; }

		void Add(IAppMapItem item);
		bool Remove(IAppMapItem item);
	}

	[DataContract]
	public abstract class SelectableAppMapContainerBase : AppMapContainerBase, ISelectableAppMapContainer
	{
		// シリアライズ
		[DataMember]
		List<IAppMapItem> _SelectedItem;
		public IReadOnlyList<IAppMapItem> SelectedItem => _SelectedItem;

		List<IAppMapItem> _SelectableItems;
		public IReadOnlyList<IAppMapItem> SelectableItems => _SelectableItems;

		List<IAppMapItem> _AllItems;
		public IReadOnlyList<IAppMapItem> AllItems => _AllItems;




		public SelectableAppMapContainerBase(HohoemaPageType pageType, string parameter = null, string label = null)
			: base(pageType, parameter, label)
		{
			_SelectedItem = new List<IAppMapItem>();
			_AllItems = new List<IAppMapItem>();
			_SelectableItems = new List<IAppMapItem>();

		}


		protected abstract Task<IEnumerable<IAppMapItem>> MakeAllItems();

		public override async Task Refresh()
		{
			var items = await MakeAllItems();
			_AllItems = items.ToList();

			// SelectedItems に既に削除されたアイテムが含まれている場合、
			// SelectedItemsからも削除する
			foreach (var selected in DisplayItems.ToArray())
			{
				if (_AllItems.All(x => !EqualAppMapItem(selected, x)))
				{
					Remove(selected);
				}
			}

			foreach (var selected in DisplayItems)
			{
                try
                {
                    if (selected is IAppMapContainer)
                    {
                        await (selected as IAppMapContainer).Refresh();
                    }
                }
                catch
                {
                    Debug.WriteLine(selected.PrimaryLabel + "の更新に失敗");
                }
            }

			// itemsからSelectedItemsを差し引いた SelectableItems を作成
			_SelectableItems.Clear();
			var selectableItems = _AllItems.Where(x => !DisplayItems.Any(y => EqualAppMapItem(x, y)));
			_SelectableItems.AddRange(selectableItems);

            Debug.WriteLine(PrimaryLabel + "を更新");
		}

		protected override async Task OnReset()
		{
			_AllItems = (await MakeAllItems()).ToList();

			_SelectableItems.Clear();
			_SelectableItems.AddRange(_AllItems);
			_DisplayItems.Clear();
		}


		public void Add(IAppMapItem item)
		{
			// このコンテナのアイテムではない
			if (!_AllItems.Any(x => EqualAppMapItem(x, item)))
			{
				return;
			}

			if (_SelectableItems.Remove(item))
			{
				_DisplayItems.Add(item);
			}
		}

		public bool Remove(IAppMapItem item)
		{
			// このコンテナのアイテムではない
			if (!_AllItems.Any(x => EqualAppMapItem(x, item)))
			{
				return false;
			}

			bool result;
			if (result = _DisplayItems.Remove(item))
			{
				_SelectableItems.Add(item);
			}

			return result;
		}
	}


	// TODO: 自分でアイテムを生み出すコンテナのベースを作成

	public interface ISelfGenerateAppMapContainer : IAppMapContainer
	{
		int DefaultDisplayCount { get; }
		int DisplayCount { get; set; }
	}

	[DataContract]
	public abstract class SelfGenerateAppMapContainerBase : AppMapContainerBase, ISelfGenerateAppMapContainer
	{
		public virtual int DefaultDisplayCount => 10;

		[DataMember]
		public int DisplayCount { get; set; }


		public SelfGenerateAppMapContainerBase(HohoemaPageType pageType, string parameter = null, string label = null)
			: base(pageType, parameter, label)
		{
			DisplayCount = DefaultDisplayCount;
		}

		public override ContainerItemDisplayType ItemDisplayType => ContainerItemDisplayType.TwoLineText;

		protected abstract Task<IEnumerable<IAppMapItem>> GenerateItems(int count);

		public override async Task Refresh()
		{
			var items = await GenerateItems(DisplayCount);
			_DisplayItems.Clear();

			foreach (var item in items.Take(DisplayCount))
			{
				_DisplayItems.Add(item);
			}
		}

		protected override Task OnReset()
		{
			DisplayCount = DefaultDisplayCount;

			return Task.CompletedTask;
		}


		


	}
}
