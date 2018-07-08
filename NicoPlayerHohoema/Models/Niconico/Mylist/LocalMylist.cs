using Prism.Commands;
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
using System.Windows.Input;
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
        public string Label
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


        public int Count => _PlaylistItems.Count;

        public string ThumnailUrl
        {
            get
            {
                var firstItem = _PlaylistItems.FirstOrDefault(x => x.Type == PlaylistItemType.Video)?.ContentId;
                if (firstItem == null) { return null; }

                var video = Database.NicoVideoDb.Get(firstItem);
                return video.ThumbnailUrl;
            }
        }

        private DelegateCommand<object> _AddItemCommand;
        public ICommand AddItemCommand => _AddItemCommand 
            ?? (_AddItemCommand = new DelegateCommand<object>((p) => 
            {
                if (p is Interfaces.IVideoContent videoItem)
                {
                    AddVideo(videoItem.Id, videoItem.Label);

                    (App.Current as App).PublishInAppNotification(
                        Models.InAppNotificationPayload.CreateReadOnlyNotification(
                            $"登録完了\r「{Label}」に「{videoItem.Label}」を追加しました"
                            ));
                }
                else if (p is string maybeVideoId)
                {
                    var info = Database.NicoVideoDb.Get(maybeVideoId);
                    if (info != null)
                    {
                        AddVideo(info.RawVideoId, info.Title);

                        (App.Current as App).PublishInAppNotification(
                        Models.InAppNotificationPayload.CreateReadOnlyNotification(
                            $"登録完了\r「{Label}」に「{info.Title}」を追加しました"
                            ));
                    }
                }
            }
            , (p) => true 
            ));


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
