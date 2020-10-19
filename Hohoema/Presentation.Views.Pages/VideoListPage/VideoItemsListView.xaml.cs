
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Prism.Ioc;
using System.Windows.Input;
using Hohoema.Presentation.Views.Flyouts;
using Hohoema.Models.UseCase.NicoVideos;
using System.Threading.Tasks;
using Reactive.Bindings.Extensions;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Niconico.UserFeature.Mylist;
using Hohoema.Models.Domain;
using I18NPortable;
using Hohoema.Presentation.ViewModels.NicoVideos.Commands;
using Hohoema.Models.Domain.Niconico;

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace Hohoema.Presentation.Views.Pages.VideoListPage
{
    public sealed partial class VideoItemsListView : UserControl
    {


        public object Header
        {
            get { return (object)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Header.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register("Header", typeof(object), typeof(VideoItemsListView), new PropertyMetadata(null));




        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ItemTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register("ItemTemplate", typeof(DataTemplate), typeof(VideoItemsListView), new PropertyMetadata(null));


        public DataTemplate ItemContextFlyoutTemplate
        {
            get { return (DataTemplate)GetValue(ItemContextFlyoutTemplateProperty); }
            set { SetValue(ItemContextFlyoutTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ItemTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemContextFlyoutTemplateProperty =
            DependencyProperty.Register("ItemContextFlyoutTemplate", typeof(DataTemplate), typeof(VideoItemsListView), new PropertyMetadata(null));





        public ICommand ItemCommand
        {
            get { return (ICommand)GetValue(ItemCommandProperty); }
            set { SetValue(ItemCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemCommandProperty =
            DependencyProperty.Register("ItemCommand", typeof(ICommand), typeof(VideoItemsListView), new PropertyMetadata(null));





        public object ItemsSource
        {
            get { return (object)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(object), typeof(VideoItemsListView), new PropertyMetadata(null));




        public ICommand RefreshCommand
        {
            get { return (ICommand)GetValue(RefreshCommandProperty); }
            set { SetValue(RefreshCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RefreshCommandProperty =
            DependencyProperty.Register("RefreshCommand", typeof(ICommand), typeof(VideoItemsListView), new PropertyMetadata(null));



        public double ScrollPosition
        {
            get { return (double)GetValue(ScrollPositionProperty); }
            set { SetValue(ScrollPositionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ScrollPosition.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ScrollPositionProperty =
            DependencyProperty.Register("ScrollPosition", typeof(double), typeof(VideoItemsListView), new PropertyMetadata(0.0));


        public IPlaylist PlaylistPassToFlyout
        {
            get { return (IPlaylist)GetValue(PlaylistPassToFlyoutProperty); }
            set { SetValue(PlaylistPassToFlyoutProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ScrollPosition.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PlaylistPassToFlyoutProperty =
            DependencyProperty.Register("PlaylistPassToFlyout", typeof(IPlaylist), typeof(VideoItemsListView), new PropertyMetadata(null));




        public void ResetScrollPosition()
        {
            var scrollViweer = ItemsList.FindFirstChild<ScrollViewer>();
            scrollViweer.ChangeView(null, 0, null);
        }



        public bool IsUpdateSourceVideoItem
        {
            get { return (bool)GetValue(IsUpdateSourceVideoItemProperty); }
            set { SetValue(IsUpdateSourceVideoItemProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsUpdateSourceVideoItem.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsUpdateSourceVideoItemProperty =
            DependencyProperty.Register("IsUpdateSourceVideoItem", typeof(bool), typeof(VideoItemsListView), new PropertyMetadata(true));




        public Thickness ItemsPanelPadding
        {
            get { return (Thickness)GetValue(ItemsPanelPaddingProperty); }
            set { SetValue(ItemsPanelPaddingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ItemsPanelPadding.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemsPanelPaddingProperty =
            DependencyProperty.Register("ItemsPanelPadding", typeof(Thickness), typeof(VideoItemsListView), new PropertyMetadata(new Thickness()));




        private readonly VideoItemsSelectionContext _selectionContext;
        private readonly NiconicoSession _niconicoSession;
        private readonly LocalMylistManager _localPlaylistManager;
        private readonly UserMylistManager _mylistManager;
        private readonly MylistAddItemCommand _addMylistCommand;
        private readonly MylistRemoveItemCommand _removeMylistCommand;
        private readonly LocalPlaylistAddItemCommand _localMylistAddCommand;
        private readonly WatchHistoryRemoveItemCommand _removeWatchHistoryCommand;
        private readonly LocalPlaylistRemoveItemCommand _localMylistRemoveCommand;
        private readonly WatchAfterAddItemCommand _addWatchAfterCommand;
        private readonly WatchAfterRemoveItemCommand _removeWatchAfterCommand;

        public VideoItemsListView()
        {
            this.InitializeComponent();

            // Selection
            _selectionContext = App.Current.Container.Resolve<VideoItemsSelectionContext>();
            _niconicoSession = App.Current.Container.Resolve<NiconicoSession>();
            _localPlaylistManager = App.Current.Container.Resolve<LocalMylistManager>();
            _mylistManager = App.Current.Container.Resolve<UserMylistManager>();

            _addWatchAfterCommand = App.Current.Container.Resolve<WatchAfterAddItemCommand>();
            _removeWatchAfterCommand = App.Current.Container.Resolve<WatchAfterRemoveItemCommand>();
            _addMylistCommand = App.Current.Container.Resolve<MylistAddItemCommand>();
            _localMylistAddCommand = new LocalPlaylistAddItemCommand();
            _removeWatchHistoryCommand = App.Current.Container.Resolve<WatchHistoryRemoveItemCommand>();
            
            Loaded += VideoItemsListView_Loaded;
            Unloaded += VideoItemsListView_Unloaded;
        }

        private void VideoItemsListView_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateSelectActionsDisplay();

            // Selection
            _selectionContext.RequestSelectAll += _selectionContext_RequestSelectAll;
            _selectionContext.SelectionStarted += _selectionContext_SelectionStarted;
            ItemsList.SelectionChanged += ItemsList_SelectionChanged;
            
            // Context Flyout
            ItemsList.ContextRequested += ItemsList_ContextRequested;
        }

        private void VideoItemsListView_Unloaded(object sender, RoutedEventArgs e)
        {
            // Selection
            _selectionContext.RequestSelectAll -= _selectionContext_RequestSelectAll;
            _selectionContext.SelectionStarted -= _selectionContext_SelectionStarted;
            ItemsList.SelectionChanged -= ItemsList_SelectionChanged;

            // Context Flyout
            ItemsList.ContextRequested -= ItemsList_ContextRequested;
        }

        #region Selection

        private void ItemsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_selectionContext.IsSelectionEnabled)
            {
                if (e.RemovedItems?.Any() ?? false)
                {
                    foreach (var removedItem in e.RemovedItems)
                    {
                        if (removedItem is IVideoContent item)
                        {
                            _selectionContext.SelectionItems.Remove(item);
                        }
                    }
                }
                if (e.AddedItems?.Any() ?? false)
                {
                    foreach (var addedItem in e.AddedItems)
                    {
                        if (addedItem is IVideoContent item)
                        {
                            if (!_selectionContext.SelectionItems.Contains(item))
                            {
                                _selectionContext.SelectionItems.Add(item);
                            }
                        }
                    }
                }

                UpdateSelectActionsDisplay();
            }
        }

        private void UpdateSelectActionsDisplay()
        {

            SelectActions_AddWatchAfter.Visibility = Visibility.Collapsed;
            SelectActions_RemoveWatchAfter.Visibility = Visibility.Collapsed;
            SelectActions_AddMylist.Visibility = Visibility.Collapsed;
            SelectActions_RemoveMylist.Visibility = Visibility.Collapsed;
            SelectActions_AddLocalMylist.Visibility = Visibility.Collapsed;
            SelectActions_RemoveLocalMylist.Visibility = Visibility.Collapsed;
            SelectActions_RemoveWatchHistory.Visibility = Visibility.Collapsed;
            SelectActions_RemoveButtonSeparator.Visibility = Visibility.Collapsed;

            if (_selectionContext.SelectionItems.Any())
            {
                SelectActions_AddWatchAfter.Visibility = Visibility.Visible;
                SelectActions_AddLocalMylist.Visibility = Visibility.Visible;

                if (PlaylistPassToFlyout?.IsWatchAfterPlaylist() ?? false)
                {
                    SelectActions_RemoveWatchAfter.Visibility = Visibility.Visible;
                    SelectActions_RemoveButtonSeparator.Visibility = Visibility.Visible;
                }
                else
                {
                    var origin = PlaylistPassToFlyout?.GetOrigin();
                    if (origin == PlaylistOrigin.Mylist && PlaylistPassToFlyout is LoginUserMylistPlaylist loginUserMylist)
                    {
                        if (_niconicoSession.IsLoggedIn)
                        {
                            SelectActions_RemoveMylist.Visibility = Visibility.Visible;
                            SelectActions_RemoveMylist.Command = new MylistRemoveItemCommand(loginUserMylist);
                            SelectActions_RemoveButtonSeparator.Visibility = Visibility.Visible;
                        }
                    }
                    else if (origin == PlaylistOrigin.Local && PlaylistPassToFlyout is LocalPlaylist localPlaylist)
                    {
                        SelectActions_RemoveLocalMylist.Visibility = Visibility.Visible;
                        SelectActions_RemoveMylist.Command = new LocalPlaylistRemoveItemCommand(localPlaylist);
                        SelectActions_RemoveButtonSeparator.Visibility = Visibility.Visible;
                    }
                }

                if (_niconicoSession.IsLoggedIn)
                {
                    SelectActions_AddMylist.Visibility = Visibility.Visible;
                }

                if (_selectionContext.SelectionItems.Any(x => x is IWatchHistory))
                {
                    SelectActions_RemoveWatchHistory.Visibility = Visibility.Visible;
                    SelectActions_RemoveButtonSeparator.Visibility = Visibility.Visible;
                }
            }
        }

        private async void _selectionContext_SelectionStarted(object sender, RequestSelectionStartEventArgs e)
        {
            await Task.Delay(50);

            var item = ItemsList.Items.FirstOrDefault(x => x == e.FirstSelectedItem);
            var container = ItemsList.ContainerFromItem(item);
            var index = ItemsList.IndexFromContainer(container);
            ItemsList.SelectRange(new ItemIndexRange(index, 1));
        }

        private void _selectionContext_RequestSelectAll(object sender, RequestSelectAllEventArgs e)
        {
            if (ItemsList.SelectedItems.Count == ItemsList.Items.Count)
            {
                foreach (var range in ItemsList.SelectedRanges.ToList())
                {
                    ItemsList.DeselectRange(range);
                }
            }
            else
            {
                ItemsList.SelectAll();
            }
        }

        #endregion



        #region Context Flyout

        private void ItemsList_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            var list = sender as ListViewBase;

            var itemFlyout = ItemContextFlyoutTemplate?.LoadContent() as FlyoutBase;
            if (itemFlyout == null) { return; }

            if (itemFlyout is VideoItemFlyout videoItemFlyout)
            {
                if (list.SelectedItems.Count > 0)
                {
                    videoItemFlyout.Playlist = PlaylistPassToFlyout;
                    videoItemFlyout.SelectedVideoItems = list.SelectedItems.Cast<IVideoContent>().ToList();
                }
                else
                {
                    videoItemFlyout.Playlist = PlaylistPassToFlyout;
                    videoItemFlyout.SelectedVideoItems = null;
                }
            }

            if (sender == args.OriginalSource) { return; }

            // コントローラ操作時にSelectorItem.DataContext == null になる
            // MenuFlyoutItemのCommand等が解決できるようにDataContextを予め埋める
            if (args.OriginalSource is SelectorItem selectorItem)
            {
                selectorItem.DataContext = selectorItem.Content;
            }

            itemFlyout.ShowAt(args.OriginalSource as FrameworkElement);
            args.Handled = true;
        }


        #endregion

    }
}
