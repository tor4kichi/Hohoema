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
    public class LegacyLocalMylist : BindableBase
    {
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
            set { SetProperty(ref _Name, value); }
        }

        [DataMember]
        private ObservableCollection<PlaylistItem> _PlaylistItems { get; set; } = new ObservableCollection<PlaylistItem>();
        public ReadOnlyObservableCollection<PlaylistItem> PlaylistItems { get; private set; }


        public int Count => _PlaylistItems.Count;

        
        public LegacyLocalMylist()
        {
            Id = null;
            PlaylistItems = new ReadOnlyObservableCollection<PlaylistItem>(_PlaylistItems);
        }

        public LegacyLocalMylist(string id, string name)
            : this()
        {
            Id = id;
            _Name = name;
        }

        [OnDeserialized]
        public void OnSeralized(StreamingContext context)
        {
            if (_PlaylistItems == null)
            {
                _PlaylistItems = new ObservableCollection<PlaylistItem>();
            }

            PlaylistItems = new ReadOnlyObservableCollection<PlaylistItem>(_PlaylistItems);
        }
    }
}
