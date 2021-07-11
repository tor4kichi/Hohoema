using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Playlist;
using Microsoft.Toolkit.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HohoemaModelTest
{
    [TestClass]
    public sealed class PlaylistTest
    {
        
        [TestMethod]
        public async Task BufferedShufflePlaylistItemsSource()
        {
            var mockSource = new MockShufflePlaylistItemsSource();
            BufferedShufflePlaylistItemsSource source = new BufferedShufflePlaylistItemsSource(mockSource, null, null);
            using (source.CollectionChangedAsObservable().Subscribe(ProcessItemsChanged))
            {
                var allItems = await source.GetPagedItemsAsync(0, 30);

                mockSource.Replace(source[5]);

                mockSource.RemoveAt(1);


            }
        }


        void ProcessItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            
        }
    }



    internal class MockShufflePlaylistItemsSource : ReadOnlyObservableCollection<IVideoContent>, IUserManagedPlaylist
    {
        public MockShufflePlaylistItemsSource()
            : base(new ObservableCollection<IVideoContent>())
        {

        }

        public int TotalCount => 100;

        public int OneTimeItemsCount => 25;

        public string Name => "Mock";

        public PlaylistId PlaylistId => new PlaylistId();

        public IPlaylistSortOption SortOption { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IPlaylistSortOption[] SortOptions => throw new NotImplementedException();

        public IPlaylistSortOption DefaultSortOption => throw new NotImplementedException();

        public void Replace(IVideoContent item)
        {
            var indexOf = Items.IndexOf(item);
            Items[indexOf] = new QueuePlaylistItem() { Id = item.VideoId };
        }

        public void RemoveAt(int i)
        {
            Items.RemoveAt(i);
        }

        public Task<IEnumerable<IVideoContent>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        {
            var start = pageIndex * OneTimeItemsCount;
            foreach (var item in Enumerable.Range(start, OneTimeItemsCount).Select((x, i) => new QueuePlaylistItem() { Id = "sm" + i}))
            {
                Items.Add(item);
            }

            var items = Items.Skip(start).Take(OneTimeItemsCount);
            return Task.FromResult(items);
        }

        public Task<IEnumerable<IVideoContent>> GetAllItemsAsync(IPlaylistSortOption sortOption, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Enumerable.Range(0, OneTimeItemsCount).Select((x, i) => new QueuePlaylistItem() { Id = "sm" + i } as IVideoContent));
        }
    }
}
