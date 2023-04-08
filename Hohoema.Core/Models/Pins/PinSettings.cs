using Hohoema.Infra;
using Hohoema.Models.PageNavigation;

namespace Hohoema.Models.Pins;

public sealed class PinSettings : LiteDBServiceBase<HohoemaPin>
{
    public PinSettings(LiteDB.LiteDatabase liteDatabase) : base(liteDatabase)
    {
        _ = _collection.EnsureIndex(nameof(HohoemaPin.PageType));
        _ = _collection.EnsureIndex(nameof(HohoemaPin.Parameter));
    }

    public bool HasPin(HohoemaPageType pageType, string parameter)
    {
        return _collection.Exists(x => x.PageType == pageType && x.Parameter == parameter);
    }

    public void RemovePin(HohoemaPageType pageType, string parameter)
    {
        _ = _collection.DeleteMany(x => x.PageType == pageType && x.Parameter == parameter);
    }

    private HohoemaPin CreatePin(string label, HohoemaPageType pageType, string parameter)
    {
        int sortIndex = _collection.Max(x => x.SortIndex);

        HohoemaPin pin = new()
        {
            Label = label,
            Parameter = parameter,
            PageType = pageType,
            SortIndex = sortIndex + 1
        };
        _ = CreateItem(pin);

        return pin;
    }
}
