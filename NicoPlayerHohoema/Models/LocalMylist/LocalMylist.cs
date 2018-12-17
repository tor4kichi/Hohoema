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
                Items.Add(videoId);
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        public Task<bool> AddMylistItem(string videoId, ContentInsertPosition insertPosition)
        {
            if (!Items.Any(x => x == videoId))
            {
                var video = Database.NicoVideoDb.Get(videoId);
                Items.Add(videoId);
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
                Items.Remove(target);
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }


        private DelegateCommand<string> _AddCommand;
        public DelegateCommand<string> AddCommand => _AddCommand
            ?? (_AddCommand = new DelegateCommand<string>((videoId) =>
            {
                Items.Add(videoId);
            }
            , (videoId) => videoId != null && !Items.Contains(videoId)
            ));


        private DelegateCommand<string> _RemoveCommand;
        public DelegateCommand<string> RemoveCommand => _RemoveCommand
            ?? (_RemoveCommand = new DelegateCommand<string>(videoId =>
            {
                Items.Remove(videoId);
            }
            , (videoId) => videoId != null && Items.Contains(videoId)
            ));
    }
}
