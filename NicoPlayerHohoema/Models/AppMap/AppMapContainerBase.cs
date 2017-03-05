using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Microsoft.Practices.Unity;
using System.Reactive.Disposables;
using NicoPlayerHohoema.Util;

namespace NicoPlayerHohoema.Models.AppMap
{
    public delegate void AppMapContainerUpdatedEventHandler(object sender, IAppMapContainer container);


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

		ContainerItemDisplayType ItemDisplayType { get; }

		Task Refresh();

        event AppMapContainerUpdatedEventHandler Updated;
    }


    [DataContract]
    public abstract class AppMapItemBase : IAppMapItem, IDisposable
    {
        [DataMember]
        public string PrimaryLabel { get; protected set; }
        [DataMember]
        public string SecondaryLabel { get; protected set; }

        [DataMember]
        public string Parameter { get; protected set; }


        public HohoemaApp HohoemaApp { get; private set; }

        public PageManager PageManager { get; private set; }


        protected CompositeDisposable _CompositeDisposable = new CompositeDisposable();

        public AppMapItemBase()
        {
            HohoemaApp = App.Current.Container.Resolve<HohoemaApp>();
            PageManager = App.Current.Container.Resolve<PageManager>();
        }

        public abstract void SelectedAction();

        public void Dispose()
        {
            _CompositeDisposable.Dispose();
        }
    }


    public abstract class VideoAppMapItemBase : AppMapItemBase
    {
        public HohoemaPlaylist Playlist { get; private set; }

        public NicoVideoQuality? Quality { get; protected set; }

        public VideoAppMapItemBase()
        {
            Playlist = HohoemaApp.Playlist;
        }

        public override void SelectedAction()
        {
            Playlist.PlayVideo(Parameter, PrimaryLabel, Quality);
        }
    }

    [DataContract]
	public abstract class AppMapContainerBase : AppMapItemBase, IAppMapContainer
    {
		protected ObservableCollection<IAppMapItem> _DisplayItems = new ObservableCollection<IAppMapItem>();
		public ReadOnlyObservableCollection<IAppMapItem> DisplayItems { get; private set; }

		public uint ItemsCount => (uint)_DisplayItems.Count;

		[DataMember]
		public HohoemaPageType PageType { get; private set; }

		public virtual ContainerItemDisplayType ItemDisplayType => ContainerItemDisplayType.Normal;


        public AppMapContainerBase ParentContainer { get; internal set; }

        public bool NowUpdating { get; private set; }
        private AsyncLock _UpdateLock = new AsyncLock();

        public event AppMapContainerUpdatedEventHandler Updated;

        public AppMapContainerBase()
        {
        }

        public AppMapContainerBase(HohoemaPageType pageType, string parameter = null, string label = null)
            : this()
		{
			DisplayItems = new ReadOnlyObservableCollection<IAppMapItem>(_DisplayItems);
			PrimaryLabel = label == null ? PageManager.PageTypeToTitle(pageType) : label;
			PageType = pageType;
			Parameter = parameter;
        }

        public override void SelectedAction()
        {
            PageManager.OpenPage(PageType, Parameter);
        }

        protected abstract Task OnRefreshing();

        public async Task Refresh()
        {
            using (var releaser = await _UpdateLock.LockAsync())
            {
                if (NowUpdating) { return; }

                NowUpdating = true;

                try
                {
                    // 自身の更新
                    await OnRefreshing();

                    // 小アイテムの更新
                    foreach (var item in DisplayItems)
                    {
                        if (item is IAppMapContainer)
                        {
                            await (item as IAppMapContainer).Refresh();
                        }
                    }

                    Updated?.Invoke(this, this);
                }
                finally
                {
                    NowUpdating = false;
                }
            }
        }

        protected bool EqualAppMapItem(IAppMapItem left, IAppMapItem right)
		{
			return left.PrimaryLabel == right.PrimaryLabel && left.Parameter == right.Parameter;
		}
    }


	
}
