using Mntone.Nico2.Videos.Thumbnail;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    public sealed class LocalMylistGroup : ObservableCollection<string>, Interfaces.ILocalMylist, INotifyPropertyChanged
    {
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

        public LocalMylistGroup(string id, string label, IEnumerable<string> initialItems = null)
            : base(initialItems ?? Enumerable.Empty<string>())
        {
            Id = id;
            Label = label;
        }

        public Task<bool> AddMylistItem(string videoId)
        {
            if (!Items.Any(x => x == videoId))
            {
                var video = Database.NicoVideoDb.Get(videoId);
                Add(videoId);
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
                    Insert(0, videoId);
                }
                else
                {
                    Add(videoId);
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
                Remove(target);
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
                Add(videoId);
            }
            , (videoId) => videoId != null && !Contains(videoId)
            ));


        private DelegateCommand<string> _RemoveItemCommand;
        public DelegateCommand<string> RemoveItemCommand => _RemoveItemCommand
            ?? (_RemoveItemCommand = new DelegateCommand<string>(videoId =>
            {
                Remove(videoId);
            }
            , (videoId) => videoId != null && Contains(videoId)
            ));

        public string ProviderId => null;

        public string ProviderName => string.Empty;

        public UserType ProviderType => UserType.User;
    }
}
