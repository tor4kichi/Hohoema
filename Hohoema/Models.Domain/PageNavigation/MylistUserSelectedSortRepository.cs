using LiteDB;
using NiconicoToolkit.Mylist;
using Hohoema.Models.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.PageNavigation
{
    public sealed class MylistUserSelectedSortRepository 
    {
        private readonly MylistUserSelectedSortRepository_Internal _mylistUserSelectedSortRepository_Internal;

        public class MylistUserSelectedSortRepository_Internal : LiteDBServiceBase<MylistUserSelectedSortEntry>
        {
            public MylistUserSelectedSortRepository_Internal(LiteDatabase liteDatabase) : base(liteDatabase)
            {
            }

            public void Set(string mylistId, MylistSortKey sortKey, MylistSortOrder sortOrder)
            {
                _collection.Upsert(new MylistUserSelectedSortEntry() { MylistId = mylistId, SortKey = sortKey, SortOrder = sortOrder });
            }

            public MylistUserSelectedSortEntry Get(string mylistId)
            {
                return _collection.FindById(mylistId);
            }
        }


        public MylistUserSelectedSortRepository(MylistUserSelectedSortRepository_Internal mylistUserSelectedSortRepository_Internal)
        {
            _mylistUserSelectedSortRepository_Internal = mylistUserSelectedSortRepository_Internal;
        }

        public void SetMylistSort(string mylistId, MylistSortKey sortKey, MylistSortOrder sortOrder)
        {
            _mylistUserSelectedSortRepository_Internal.Set(mylistId, sortKey, sortOrder);
        }

        public (MylistSortKey? SortKey, MylistSortOrder? SortOrder) GetMylistSort(string mylistId)
        {
            var entry = _mylistUserSelectedSortRepository_Internal.Get(mylistId);
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
}
