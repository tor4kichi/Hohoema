#nullable enable
using Hohoema.Infra;
using LiteDB;
using NiconicoToolkit.Mylist;

namespace Hohoema.Models.PageNavigation;

public sealed class MylistUserSelectedSortRepository
{
    private readonly MylistUserSelectedSortRepository_Internal _mylistUserSelectedSortRepository_Internal;

    public class MylistUserSelectedSortRepository_Internal : LiteDBServiceBase<MylistUserSelectedSortEntry>
    {
        public MylistUserSelectedSortRepository_Internal(LiteDatabase liteDatabase) : base(liteDatabase)
        {
        }

        public void Set(MylistId mylistId, MylistSortKey sortKey, MylistSortOrder sortOrder)
        {
            _ = _collection.Upsert(new MylistUserSelectedSortEntry() { MylistId = mylistId, SortKey = sortKey, SortOrder = sortOrder });
        }

        public MylistUserSelectedSortEntry Get(MylistId mylistId)
        {
            return _collection.FindById(mylistId.ToString());
        }
    }


    public MylistUserSelectedSortRepository(MylistUserSelectedSortRepository_Internal mylistUserSelectedSortRepository_Internal)
    {
        _mylistUserSelectedSortRepository_Internal = mylistUserSelectedSortRepository_Internal;
    }

    public void SetMylistSort(MylistId mylistId, MylistSortKey sortKey, MylistSortOrder sortOrder)
    {
        _mylistUserSelectedSortRepository_Internal.Set(mylistId, sortKey, sortOrder);
    }

    public (MylistSortKey? SortKey, MylistSortOrder? SortOrder) GetMylistSort(MylistId mylistId)
    {
        MylistUserSelectedSortEntry entry = _mylistUserSelectedSortRepository_Internal.Get(mylistId);
        return (entry?.SortKey, entry?.SortOrder);
    }
}

public class MylistUserSelectedSortEntry
{
    [BsonId]
    public string MylistId { get; set; }

    [BsonField]
    public MylistSortKey SortKey { get; set; }

    [BsonField]
    public MylistSortOrder SortOrder { get; set; }
}
