using CommunityToolkit.Mvvm.ComponentModel;
using Hohoema.Models.PageNavigation;
using LiteDB;
using System.Collections.Generic;

namespace Hohoema.Models.Pins;

public sealed class HohoemaPin : ObservableObject
{
    [BsonId(autoId: true)]
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
        get => _OverrideLabel;
        set => SetProperty(ref _OverrideLabel, value);
    }


    [BsonField]
    public int SortIndex { get; set; }

    [BsonField]
    public BookmarkType PinType { get; set; } = BookmarkType.Item;

    [BsonField]
    public List<HohoemaPin> SubItems { get; set; } = new List<HohoemaPin>();

    [BsonField]
    public bool IsOpenSubItems { get; set; }
}

public enum BookmarkType
{
    Item,
    Folder,
}
