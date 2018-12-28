using Mntone.Nico2.Videos.Thumbnail;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.LocalMylist
{
    public enum ContentInsertPosition
    {
        Head,
        Tail,
    }

    public sealed class LocalMylistGroup : ReadOnlyObservableCollection<string>, Interfaces.ILocalMylist, INotifyPropertyChanged, INotifyCollectionChanged
    {
        
        public LocalMylistGroup(string id, string label, ObservableCollection<string> initialItems = null)
            : base(initialItems ?? (initialItems = new ObservableCollection<string>()))
        {
            OriginalItems = initialItems;
            Id = id;
            Label = label;
        }


        private ObservableCollection<string> OriginalItems;

        public string Id { get; internal set; }

        public int SortIndex { get; internal set; }

        string _Label;
        public string Label
        {
            get { return _Label; }
            set
            {
                if (_Label != value)
                {
                    _Label = value;
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(Label)));
                }
            }
        }

        public int ItemCount => Count;

        public Task<bool> AddMylistItem(string videoId)
        {
            if (!Items.Any(x => x == videoId))
            {
                var video = Database.NicoVideoDb.Get(videoId);
                OriginalItems.Add(videoId);
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        public Task<bool> AddMylistItem(string videoId, ContentInsertPosition insertPosition = ContentInsertPosition.Tail)
        {
            if (!Items.Any(x => x == videoId))
            {
                var video = Database.NicoVideoDb.Get(videoId);
                if (insertPosition == ContentInsertPosition.Head)
                {
                    OriginalItems.Insert(0, videoId);
                }
                else
                {
                    OriginalItems.Add(videoId);
                }
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        public Task<bool> RemoveMylistItem(string videoId)
        {
            var target = Items.SingleOrDefault(x => x == videoId);
            if (target != null)
            {
                OriginalItems.Remove(target);
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }


        private DelegateCommand<string> _AddItemCommand;
        public DelegateCommand<string> AddItemCommand => _AddItemCommand
            ?? (_AddItemCommand = new DelegateCommand<string>((videoId) =>
            {
                AddMylistItem(videoId);
            }
            , (videoId) => videoId != null && !Contains(videoId)
            ));


        private DelegateCommand<string> _RemoveItemCommand;
        public DelegateCommand<string> RemoveItemCommand => _RemoveItemCommand
            ?? (_RemoveItemCommand = new DelegateCommand<string>(videoId =>
            {
                RemoveMylistItem(videoId);
            }
            , (videoId) => videoId != null && Contains(videoId)
            ));

        public string ProviderId => null;

        public string ProviderName => string.Empty;

        public UserType ProviderType => UserType.User;
    }
}
