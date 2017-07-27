using Prism.Mvvm;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;

namespace NicoPlayerHohoema.Models
{
    [DataContract]
    public class LocalMylist : BindableBase, IPlayableList, IDisposable
    {
        public HohoemaPlaylist HohoemaPlaylist { get; internal set; }


        public PlaylistOrigin Origin => PlaylistOrigin.Local;

        [DataMember]
        public int SortIndex { get; internal set; }


        [DataMember]
        public string Id { get; private set; }


        private string _Name;

        [DataMember]
        public string Name
        {
            get { return _Name; }
            set
            {
                if (SetProperty(ref _Name, value))
                {
                    // プレイリストの名前が変更されたらファイルの名前も変更
                    if (HohoemaPlaylist != null)
                    {
                        HohoemaPlaylist.RenamePlaylist(this, _Name);
                    }
                }
            }
        }

        [DataMember]
        private ObservableCollection<PlaylistItem> _PlaylistItems { get; set; } = new ObservableCollection<PlaylistItem>();
        public ReadOnlyObservableCollection<PlaylistItem> PlaylistItems { get; private set; }

        public LocalMylist()
        {
            Id = null;
            PlaylistItems = new ReadOnlyObservableCollection<PlaylistItem>(_PlaylistItems);
        }

        public LocalMylist(string id, string name)
            : this()
        {
            Id = id;
            _Name = name;
        }


        public void Dispose()
        {
        }


        [OnDeserialized]
        public void OnSeralized(StreamingContext context)
        {
            foreach (var item in PlaylistItems)
            {
                item.Owner = this;
            }

            if (_PlaylistItems == null)
            {
                _PlaylistItems = new ObservableCollection<PlaylistItem>();
            }

            PlaylistItems = new ReadOnlyObservableCollection<PlaylistItem>(_PlaylistItems);
        }

        public PlaylistItem AddVideo(string contentId, string contentName = null, ContentInsertPosition insertPosition = ContentInsertPosition.Tail)
        {
            if (contentId == null) { throw new Exception(); }

            // すでに登録済みの場合
            var alreadyAdded = _PlaylistItems.SingleOrDefault(x => x.Type == PlaylistItemType.Video && x.ContentId == contentId);
            if (alreadyAdded != null)
            {
                // 何もしない
                return alreadyAdded;
            }

            var newItem = new QualityVideoPlaylistItem()
            {
                Type = PlaylistItemType.Video,
                ContentId = contentId,
                Title = contentName,
                Quality = null,
                Owner = this,
            };

            if (insertPosition == ContentInsertPosition.Head)
            {
                _PlaylistItems.Insert(0, newItem);
            }
            else
            {
                _PlaylistItems.Add(newItem);
            }


            HohoemaPlaylist.Save(this).ConfigureAwait(false);

            return newItem;
        }

       

        public bool Remove(PlaylistItem item)
        {
            if (PlaylistItems.Contains(item))
            {
                if (_PlaylistItems.Remove(item))
                {
                    HohoemaPlaylist.Save(this).ConfigureAwait(false);

                    return true;
                }
            }

            return false;
        }

    }
}
