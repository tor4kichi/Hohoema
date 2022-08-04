using Hohoema.Models.Domain.PageNavigation;
using LiteDB;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using System.Collections.Generic;

namespace Hohoema.Models.Domain.Pins
{
    public sealed class HohoemaPin : ObservableObject
    {
        [BsonId(autoId:true)]
        public int Id { get; set; }

        [BsonField]
        public HohoemaPageType PageType { get; set; }
        [BsonField]
        public string Parameter { get; set; }
        [BsonField]
        public string Label { get; set; }

        private string _OverrideLabel;
        [BsonField]
        public string OverrideLabel
        {
            get { return _OverrideLabel; }
            set { SetProperty(ref _OverrideLabel, value); }
        }


        [BsonField]
        public int SortIndex { get; set; }

        [BsonField]
        public BookmarkType PinType { get; set; } = BookmarkType.Item;

        [BsonField]
        public List<HohoemaPin> SubItems { get; set; } = new List<HohoemaPin>();
    }

    public enum BookmarkType
    {
        Item,
        Folder,
    }
}
